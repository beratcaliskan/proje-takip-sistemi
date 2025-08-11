using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Models;

namespace ProjeTakip.Pages.Reports
{
    public class IndexModel : PageModel
    {
        private readonly ProjeTakipContext _context;

        public IndexModel(ProjeTakipContext context)
        {
            _context = context;
        }

        // Genel İstatistikler
        public int ToplamProje { get; set; }
        public int AktifProje { get; set; }
        public int TamamlananProje { get; set; }
        public int IptalEdilenProje { get; set; }
        public int ToplamKullanici { get; set; }
        public decimal ToplamMaliyet { get; set; }
        public decimal OrtalamaMaliyet { get; set; }

        // Durum Bazlı Projeler
        public List<Proje> OnayBekleyenProjeler { get; set; } = new();
        public List<Proje> PlanlamaProjeler { get; set; } = new();
        public List<Proje> DevamEdenProjeler { get; set; } = new();
        public List<Proje> TamamlananProjeler { get; set; } = new();
        public List<Proje> IptalEdilenProjeler { get; set; } = new();

        // Müdürlük Bazlı İstatistikler
        public List<MudurlukIstatistik> MudurlukIstatistikleri { get; set; } = new();

        // Departman Bazlı İstatistikler
        public List<DepartmanIstatistik> DepartmanIstatistikleri { get; set; } = new();

        // Aylık Proje Dağılımı
        public List<AylikIstatistik> AylikProjeDagilimi { get; set; } = new();
        public List<AylikIstatistik> AylikIstatistikler { get; set; } = new();
        
        // Tüm Projeler
        public List<Proje> TumProjeler { get; set; } = new();

        // Gantt Aşama İstatistikleri
        public List<AsamaIstatistik> AsamaIstatistikleri { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Genel İstatistikler
            var projeler = await _context.Projeler.ToListAsync();
            var kullanicilar = await _context.Kullanicilar.ToListAsync();
            var ganttAsamalari = await _context.GanttAsamalari.Include(g => g.Proje).ToListAsync();

            ToplamProje = projeler.Count;
            AktifProje = projeler.Count(p => p.Durum == 2 || p.Durum == 3); // Planlama + Devam Ediyor
            TamamlananProje = projeler.Count(p => p.Durum == 4);
            IptalEdilenProje = projeler.Count(p => p.Durum == 5);
            ToplamKullanici = kullanicilar.Count;
            ToplamMaliyet = projeler.Where(p => p.Maliyet.HasValue).Sum(p => p.Maliyet!.Value);
            OrtalamaMaliyet = projeler.Where(p => p.Maliyet.HasValue).Any() ? 
                projeler.Where(p => p.Maliyet.HasValue).Average(p => p.Maliyet!.Value) : 0;

            // Durum Bazlı Projeler
            OnayBekleyenProjeler = await _context.Projeler
                .Where(p => p.Durum == 1)
                .OrderByDescending(p => p.bas)
                .ToListAsync();

            PlanlamaProjeler = await _context.Projeler
                .Where(p => p.Durum == 2)
                .OrderByDescending(p => p.bas)
                .ToListAsync();

            DevamEdenProjeler = await _context.Projeler
                .Where(p => p.Durum == 3)
                .OrderByDescending(p => p.bas)
                .ToListAsync();

            TamamlananProjeler = await _context.Projeler
                .Where(p => p.Durum == 4)
                .OrderByDescending(p => p.bas)
                .ToListAsync();

            IptalEdilenProjeler = projeler.Where(p => p.Durum == 5).ToList();

            // Müdürlük Bazlı İstatistikler
            MudurlukIstatistikleri = projeler
                .GroupBy(p => p.Mudurluk)
                .Select(g => new MudurlukIstatistik
                {
                    MudurlukAdi = g.Key,
                    ToplamProje = g.Count(),
                    AktifProje = g.Count(p => p.Durum == 2 || p.Durum == 3),
                    TamamlananProje = g.Count(p => p.Durum == 4),
                    ToplamMaliyet = g.Where(p => p.Maliyet.HasValue).Sum(p => p.Maliyet!.Value)
                })
                .OrderByDescending(m => m.ToplamProje)
                .ToList();

            // Departman Bazlı İstatistikler
            DepartmanIstatistikleri = projeler
                .GroupBy(p => p.Mudurluk)
                .Select(g => new DepartmanIstatistik
                {
                    DepartmanAdi = g.Key ?? "Belirtilmemiş",
                    ProjeSayisi = g.Count(),
                    ToplamMaliyet = g.Where(p => p.Maliyet.HasValue).Sum(p => p.Maliyet!.Value),
                    OrtalamaMaliyet = g.Where(p => p.Maliyet.HasValue).Any() ? 
                        g.Where(p => p.Maliyet.HasValue).Average(p => p.Maliyet!.Value) : 0
                })
                .OrderByDescending(d => d.ProjeSayisi)
                .ToList();

            // Aylık Proje Dağılımı (Son 12 ay)
            var sonBirYil = DateTime.Now.AddMonths(-12);
            AylikProjeDagilimi = projeler
                .Where(p => p.bas.HasValue && p.bas >= sonBirYil)
                .GroupBy(p => new { Year = p.bas!.Value.Year, Month = p.bas!.Value.Month })
                .Select(g => new AylikIstatistik
                {
                    Yil = g.Key.Year,
                    Ay = g.Key.Month,
                    ProjeAdedi = g.Count(),
                    ToplamMaliyet = g.Where(p => p.Maliyet.HasValue).Sum(p => p.Maliyet!.Value)
                })
                .OrderBy(a => a.Yil).ThenBy(a => a.Ay)
                .ToList();
                
            // AylikIstatistikler property'sini de doldur
            AylikIstatistikler = AylikProjeDagilimi;
            
            // TumProjeler property'sini doldur
            TumProjeler = projeler;

            // Aşama İstatistikleri
            AsamaIstatistikleri = ganttAsamalari
                .GroupBy(g => g.Asama)
                .Select(g => new AsamaIstatistik
                {
                    AsamaAdi = g.Key,
                    ToplamAdet = g.Count(),
                    OrtalamaSure = g.Average(a => a.Gun),
                    TamamlananAdet = g.Count(a => a.Bitis.HasValue && a.Bitis.Value <= DateTime.Now)
                })
                .OrderByDescending(a => a.ToplamAdet)
                .ToList();
        }
    }

    public class MudurlukIstatistik
    {
        public string MudurlukAdi { get; set; } = string.Empty;
        public int ToplamProje { get; set; }
        public int AktifProje { get; set; }
        public int TamamlananProje { get; set; }
        public decimal ToplamMaliyet { get; set; }
        public decimal BasariOrani => ToplamProje > 0 ? (decimal)TamamlananProje / ToplamProje * 100 : 0;
    }

    public class DepartmanIstatistik
    {
        public string DepartmanAdi { get; set; } = string.Empty;
        public int ProjeSayisi { get; set; }
        public decimal ToplamMaliyet { get; set; }
        public decimal OrtalamaMaliyet { get; set; }
    }

    public class AylikIstatistik
    {
        public int Yil { get; set; }
        public int Ay { get; set; }
        public int ProjeAdedi { get; set; }
        public decimal ToplamMaliyet { get; set; }
        public decimal OrtalamaMaliyet => ProjeAdedi > 0 ? ToplamMaliyet / ProjeAdedi : 0;
        public int ProjeSayisi => ProjeAdedi;
        public string AyAdi => new DateTime(Yil, Ay, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
    }

    public class AsamaIstatistik
    {
        public string AsamaAdi { get; set; } = string.Empty;
        public int ToplamAdet { get; set; }
        public double OrtalamaSure { get; set; }
        public int TamamlananAdet { get; set; }
        public int ProjeSayisi => ToplamAdet;
        public double Yuzde => ToplamAdet > 0 ? (double)TamamlananAdet / ToplamAdet * 100 : 0;
        public double TamamlanmaOrani => ToplamAdet > 0 ? (double)TamamlananAdet / ToplamAdet * 100 : 0;
    }
}