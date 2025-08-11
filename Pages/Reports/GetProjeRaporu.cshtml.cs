using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Models;

namespace ProjeTakip.Pages.Reports
{
    public class GetProjeRaporuModel : PageModel
    {
        private readonly ProjeTakipContext _context;

        public GetProjeRaporuModel(ProjeTakipContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync(int projeId)
        {
            try
            {
                // Seçilen projeyi getir
                var secilenProje = await _context.Projeler
                    .FirstOrDefaultAsync(p => p.ProjeID == projeId);

                if (secilenProje == null)
                {
                    return NotFound(new { error = "Proje bulunamadı" });
                }

                // Tüm verileri çek
                var tumProjeler = await _context.Projeler.ToListAsync();
                var ganttAsamalari = await _context.GanttAsamalari
                    .Include(g => g.Proje)
                    .ToListAsync();

                // Genel istatistikler
                var genelIstatistikler = new
                {
                    toplamProje = tumProjeler.Count,
                    aktifProje = tumProjeler.Count(p => p.Durum == 2 || p.Durum == 3),
                    tamamlananProje = tumProjeler.Count(p => p.Durum == 4),
                    iptalEdilenProje = tumProjeler.Count(p => p.Durum == 5),
                    toplamMaliyet = tumProjeler.Where(p => p.Maliyet.HasValue).Sum(p => p.Maliyet!.Value)
                };

                // Müdürlük bazlı istatistikler
                var mudurlukIstatistikleri = tumProjeler
                    .GroupBy(p => p.Mudurluk)
                    .Select(g => new
                    {
                        mudurlukAdi = g.Key ?? "Belirtilmemiş",
                        toplamProje = g.Count(),
                        aktifProje = g.Count(p => p.Durum == 2 || p.Durum == 3),
                        tamamlananProje = g.Count(p => p.Durum == 4),
                        toplamMaliyet = g.Where(p => p.Maliyet.HasValue).Sum(p => p.Maliyet!.Value),
                        basariOrani = g.Count() > 0 ? (decimal)g.Count(p => p.Durum == 4) / g.Count() * 100 : 0
                    })
                    .OrderByDescending(m => m.toplamProje)
                    .ToList();

                // Departman bazlı istatistikler
                var departmanIstatistikleri = tumProjeler
                    .GroupBy(p => p.Baskanlik ?? p.Mudurluk)
                    .Select(g => new
                    {
                        departmanAdi = g.Key ?? "Belirtilmemiş",
                        projeSayisi = g.Count(),
                        toplamMaliyet = g.Where(p => p.Maliyet.HasValue).Sum(p => p.Maliyet!.Value),
                        ortalamaMaliyet = g.Where(p => p.Maliyet.HasValue).Any() ?
                            g.Where(p => p.Maliyet.HasValue).Average(p => p.Maliyet!.Value) : 0
                    })
                    .OrderByDescending(d => d.projeSayisi)
                    .ToList();

                // Aylık dağılım (son 12 ay)
                var sonBirYil = DateTime.Now.AddMonths(-12);
                var aylikDagilim = tumProjeler
                    .Where(p => p.bas.HasValue && p.bas >= sonBirYil)
                    .GroupBy(p => new { Year = p.bas!.Value.Year, Month = p.bas!.Value.Month })
                    .Select(g => new
                    {
                        yil = g.Key.Year,
                        ay = g.Key.Month,
                        ayAdi = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("tr-TR")),
                        projeSayisi = g.Count(),
                        toplamMaliyet = g.Where(p => p.Maliyet.HasValue).Sum(p => p.Maliyet!.Value),
                        ortalamaMaliyet = g.Where(p => p.Maliyet.HasValue).Any() ?
                            g.Where(p => p.Maliyet.HasValue).Average(p => p.Maliyet!.Value) : 0
                    })
                    .OrderBy(a => a.yil).ThenBy(a => a.ay)
                    .ToList();

                // Aşama istatistikleri
                var asamaIstatistikleri = ganttAsamalari
                    .GroupBy(g => g.Asama)
                    .Select(g => new
                    {
                        asamaAdi = g.Key ?? "Belirtilmemiş",
                        toplamAdet = g.Count(),
                        ortalamaSure = g.Average(a => a.Gun),
                        tamamlananAdet = g.Count(a => a.Bitis.HasValue && a.Bitis.Value <= DateTime.Now),
                        yuzde = g.Count() > 0 ? (double)g.Count(a => a.Bitis.HasValue && a.Bitis.Value <= DateTime.Now) / g.Count() * 100 : 0
                    })
                    .OrderByDescending(a => a.toplamAdet)
                    .ToList();

                // Durum bazlı projeler
                var durumBazliProjeler = new
                {
                    onayBekleyen = tumProjeler.Where(p => p.Durum == 1).ToList(),
                    planlama = tumProjeler.Where(p => p.Durum == 2).ToList(),
                    devamEden = tumProjeler.Where(p => p.Durum == 3).ToList(),
                    tamamlanan = tumProjeler.Where(p => p.Durum == 4).ToList(),
                    iptalEdilen = tumProjeler.Where(p => p.Durum == 5).ToList()
                };

                // Seçilen projeye ait Gantt aşamaları
                var projeAsamalari = ganttAsamalari
                    .Where(g => g.ProjeID == projeId)
                    .OrderBy(g => g.Baslangic)
                    .ToList();

                var raporData = new
                {
                    proje = new
                    {
                        projeId = secilenProje.ProjeID,
                        projeAd = secilenProje.ProjeAd,
                        mudurluk = secilenProje.Mudurluk,
                        baskanlik = secilenProje.Baskanlik,
                        durum = secilenProje.Durum,
                        durumAdi = secilenProje.DurumAdi,
                        baslangicTarihi = secilenProje.bas?.ToString("dd.MM.yyyy"),
                        bitisTarihi = secilenProje.bit?.ToString("dd.MM.yyyy"),
                        maliyet = secilenProje.Maliyet,
                        maliyetFormatli = secilenProje.MaliyetFormatli
                    },
                    genelIstatistikler,
                    mudurlukIstatistikleri,
                    departmanIstatistikleri,
                    aylikDagilim,
                    asamaIstatistikleri,
                    durumBazliProjeler,
                    projeAsamalari = projeAsamalari.Select(a => new
                    {
                        asama = a.Asama,
                        baslangic = a.Baslangic?.ToString("dd.MM.yyyy"),
                        bitis = a.Bitis?.ToString("dd.MM.yyyy"),
                        gun = a.Gun,
                        tamamlanmaDurumu = a.Bitis.HasValue && a.Bitis.Value <= DateTime.Now ? "Tamamlandı" : "Devam Ediyor"
                    }).ToList(),
                    tumProjeler = tumProjeler.Select(p => new
                    {
                        projeId = p.ProjeID,
                        projeAd = p.ProjeAd,
                        mudurluk = p.Mudurluk,
                        baskanlik = p.Baskanlik,
                        durum = p.Durum,
                        durumAdi = p.DurumAdi,
                        baslangicTarihi = p.bas?.ToString("dd.MM.yyyy"),
                        bitisTarihi = p.bit?.ToString("dd.MM.yyyy"),
                        maliyet = p.Maliyet,
                        maliyetFormatli = p.MaliyetFormatli
                    }).ToList()
                };

                return new JsonResult(raporData);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}