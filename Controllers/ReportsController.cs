using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Models;

namespace ProjeTakip.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly ProjeTakipContext _context;

        public ReportsController(ProjeTakipContext context)
        {
            _context = context;
        }

        [HttpGet("GetProjeRaporu")]
        public async Task<IActionResult> GetProjeRaporu(int? projeId)
        {
            try
            {
                // Proje ID kontrolü
                if (!projeId.HasValue || projeId.Value <= 0)
                {
                    return BadRequest(new { error = "Geçerli bir proje ID'si gereklidir" });
                }

                // Seçilen projeyi getir
                var secilenProje = await _context.Projeler
                    .FirstOrDefaultAsync(p => p.ProjeID == projeId.Value);

                if (secilenProje == null)
                {
                    return NotFound(new { error = $"Proje bulunamadı (ID: {projeId.Value})" });
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
                        projeSayisi = g.Count(),
                        yuzde = g.Count() > 0 ? (double)g.Count(a => a.Bitis.HasValue && a.Bitis.Value <= DateTime.Now) / g.Count() * 100 : 0
                    })
                    .OrderByDescending(a => a.projeSayisi)
                    .ToList();

                var raporData = new
                {
                    proje = new
                    {
                        projeID = secilenProje.ProjeID,
                        projeAd = secilenProje.ProjeAd,
                        mudurluk = secilenProje.Mudurluk,
                        departman = secilenProje.Baskanlik,
                        durum = secilenProje.Durum,
                        baslangicTarihi = secilenProje.bas,
                        bitisTarihi = secilenProje.bit,
                        maliyet = secilenProje.Maliyet
                    },
                    genelIstatistikler,
                    mudurlukIstatistikleri,
                    departmanIstatistikleri,
                    aylikDagilim,
                    asamaIstatistikleri,
                    tumProjeler = tumProjeler.Select(p => new
                    {
                        projeID = p.ProjeID,
                        projeAd = p.ProjeAd,
                        mudurluk = p.Mudurluk,
                        departman = p.Baskanlik,
                        durum = p.Durum,
                        baslangicTarihi = p.bas,
                        bitisTarihi = p.bit,
                        maliyet = p.Maliyet
                    }).ToList()
                };

                return Ok(raporData);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("GetGenelRapor")]
        public async Task<IActionResult> GetGenelRapor(string? kategori = null, string? deger = null)
        {
            try
            {
                // Tüm verileri çek
                var tumProjeler = await _context.Projeler.ToListAsync();
                var ganttAsamalari = await _context.GanttAsamalari
                    .Include(g => g.Proje)
                    .ToListAsync();

                // Kategori bazlı filtreleme
                var filtreliProjeler = tumProjeler;
                if (!string.IsNullOrEmpty(kategori) && !string.IsNullOrEmpty(deger))
                {
                    switch (kategori.ToLower())
                    {
                        case "mudurluk":
                            filtreliProjeler = tumProjeler.Where(p => 
                                string.Equals(p.Mudurluk, deger, StringComparison.OrdinalIgnoreCase)).ToList();
                            break;
                        case "departman":
                            filtreliProjeler = tumProjeler.Where(p => 
                                string.Equals(p.Baskanlik, deger, StringComparison.OrdinalIgnoreCase)).ToList();
                            break;
                        case "durum":
                            if (int.TryParse(deger, out int durumId))
                            {
                                filtreliProjeler = tumProjeler.Where(p => p.Durum == durumId).ToList();
                            }
                            break;
                    }
                }

                // Genel istatistikler
                var genelIstatistikler = new
                {
                    toplamProje = filtreliProjeler.Count,
                    aktifProje = filtreliProjeler.Count(p => p.Durum == 2 || p.Durum == 3),
                    tamamlananProje = filtreliProjeler.Count(p => p.Durum == 4),
                    iptalEdilenProje = filtreliProjeler.Count(p => p.Durum == 5),
                    toplamMaliyet = filtreliProjeler.Where(p => p.Maliyet.HasValue).Sum(p => p.Maliyet!.Value)
                };

                // Müdürlük bazlı istatistikler
                var mudurlukIstatistikleri = filtreliProjeler
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
                var departmanIstatistikleri = filtreliProjeler
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
                var aylikDagilim = filtreliProjeler
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
                    .Where(g => filtreliProjeler.Any(p => p.ProjeID == g.ProjeID))
                    .GroupBy(g => g.Asama)
                    .Select(g => new
                    {
                        asamaAdi = g.Key ?? "Belirtilmemiş",
                        projeSayisi = g.Count(),
                        yuzde = g.Count() > 0 ? (double)g.Count(a => a.Bitis.HasValue && a.Bitis.Value <= DateTime.Now) / g.Count() * 100 : 0
                    })
                    .OrderByDescending(a => a.projeSayisi)
                    .ToList();

                var raporData = new
                {
                    kategori = kategori ?? "Genel",
                    deger = deger ?? "Tüm Projeler",
                    genelIstatistikler,
                    mudurlukIstatistikleri,
                    departmanIstatistikleri,
                    aylikDagilim,
                    asamaIstatistikleri,
                    tumProjeler = filtreliProjeler.Select(p => new
                    {
                        projeID = p.ProjeID,
                        projeAd = p.ProjeAd,
                        mudurluk = p.Mudurluk,
                        departman = p.Baskanlik,
                        durum = p.Durum,
                        baslangicTarihi = p.bas,
                        bitisTarihi = p.bit,
                        maliyet = p.Maliyet
                    }).ToList()
                };

                return Ok(raporData);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}