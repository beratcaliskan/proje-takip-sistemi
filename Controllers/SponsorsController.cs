using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Models;
using ProjeTakip.Services;

namespace ProjeTakip.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SponsorsController : ControllerBase
    {
        private readonly ProjeTakipContext _context;
        private readonly SystemLogService _systemLogService;

        public SponsorsController(ProjeTakipContext context, SystemLogService systemLogService)
        {
            _context = context;
            _systemLogService = systemLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetSponsors()
        {
            // Debug için basit test
            Console.WriteLine("GetSponsors API çağrıldı!");
            return Ok(new { success = true, message = "API çalışıyor", data = new[] { new { id = 1, sponsorAd = "Test Sponsor" } } });
        }

        [HttpGet("full")]
        public async Task<IActionResult> GetSponsorsFullOld()
        {
            try
            {
                var sponsorlar = await _context.Sponsorler
                    .OrderBy(s => s.SponsorAd)
                    .Select(s => new
                    {
                        id = s.id,
                        sponsorAd = s.SponsorAd,
                        iletisimBilgisi = s.IletisimBilgisi,
                        aciklama = s.Aciklama
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = sponsorlar,
                    count = sponsorlar.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSponsor(int id)
        {
            try
            {
                var sponsor = await _context.Sponsorler.FindAsync(id);
                if (sponsor == null)
                {
                    return NotFound(new { success = false, message = "Sponsor bulunamadı" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = sponsor.id,
                        sponsorAd = sponsor.SponsorAd,
                        iletisimBilgisi = sponsor.IletisimBilgisi,
                        aciklama = sponsor.Aciklama
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateSponsor([FromBody] CreateSponsorRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.SponsorAd))
                {
                    return BadRequest(new { success = false, message = "Sponsor adı gereklidir" });
                }

                // Aynı isimde sponsor kontrolü
                var mevcutSponsor = await _context.Sponsorler
                    .FirstOrDefaultAsync(s => s.SponsorAd.ToLower() == request.SponsorAd.ToLower());
                
                if (mevcutSponsor != null)
                {
                    return BadRequest(new { success = false, message = "Bu sponsor adı zaten kullanılıyor" });
                }

                var sponsor = new Sponsor
                {
                    SponsorAd = request.SponsorAd,
                    IletisimBilgisi = request.IletisimBilgisi,
                    Aciklama = request.Aciklama
                };

                _context.Sponsorler.Add(sponsor);
                await _context.SaveChangesAsync();

                // Log kaydı
                await _systemLogService.LogAsync(
                    "Sponsor Eklendi",
                    $"Yeni sponsor eklendi: {sponsor.SponsorAd}",
                    "System",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                );

                return Ok(new
                {
                    success = true,
                    message = "Sponsor başarıyla oluşturuldu",
                    data = new { id = sponsor.id }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSponsor(int id, [FromBody] UpdateSponsorRequest request)
        {
            try
            {
                var sponsor = await _context.Sponsorler.FindAsync(id);
                if (sponsor == null)
                {
                    return NotFound(new { success = false, message = "Sponsor bulunamadı" });
                }

                if (string.IsNullOrEmpty(request.SponsorAd))
                {
                    return BadRequest(new { success = false, message = "Sponsor adı gereklidir" });
                }

                // Aynı isimde başka sponsor kontrolü
                var mevcutSponsor = await _context.Sponsorler
                    .FirstOrDefaultAsync(s => s.SponsorAd.ToLower() == request.SponsorAd.ToLower() && s.id != id);
                
                if (mevcutSponsor != null)
                {
                    return BadRequest(new { success = false, message = "Bu sponsor adı zaten kullanılıyor" });
                }

                var eskiAd = sponsor.SponsorAd;
                sponsor.SponsorAd = request.SponsorAd;
                sponsor.IletisimBilgisi = request.IletisimBilgisi;
                sponsor.Aciklama = request.Aciklama;
                
                await _context.SaveChangesAsync();

                // Log kaydı
                await _systemLogService.LogAsync(
                    "Sponsor Güncellendi",
                    $"Sponsor güncellendi: {eskiAd} -> {sponsor.SponsorAd}",
                    "System",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                );

                return Ok(new { success = true, message = "Sponsor başarıyla güncellendi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSponsor(int id)
        {
            try
            {
                var sponsor = await _context.Sponsorler.FindAsync(id);
                if (sponsor == null)
                {
                    return NotFound(new { success = false, message = "Sponsor bulunamadı" });
                }

                // Bu sponsora bağlı projeler var mı kontrol et (sponsor string field olduğu için string karşılaştırması)
                var bagliProjeler = await _context.Projeler
                    .Where(p => p.sponsor != null && p.sponsor.Contains(sponsor.SponsorAd))
                    .CountAsync();

                if (bagliProjeler > 0)
                {
                    return BadRequest(new 
                    { 
                        success = false, 
                        message = $"Bu sponsora bağlı {bagliProjeler} adet proje bulunduğu için silinemez" 
                    });
                }

                var sponsorAd = sponsor.SponsorAd;
                _context.Sponsorler.Remove(sponsor);
                await _context.SaveChangesAsync();

                // Log kaydı
                await _systemLogService.LogAsync(
                    "Sponsor Silindi",
                    $"Sponsor silindi: {sponsorAd}",
                    "System",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                );

                return Ok(new { success = true, message = "Sponsor başarıyla silindi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetSponsorStatistics()
        {
            try
            {
                var sponsorlar = await _context.Sponsorler.ToListAsync();
                var projeler = await _context.Projeler.ToListAsync();

                var statistics = sponsorlar.Select(s => new
                {
                    id = s.id,
                    sponsorAd = s.SponsorAd,
                    toplamProje = projeler.Count(p => p.sponsor != null && p.sponsor.Contains(s.SponsorAd)),
                    aktifProje = projeler.Count(p => p.sponsor != null && p.sponsor.Contains(s.SponsorAd) && (p.Durum == 2 || p.Durum == 3)),
                    tamamlananProje = projeler.Count(p => p.sponsor != null && p.sponsor.Contains(s.SponsorAd) && p.Durum == 4),
                    toplamMaliyet = projeler
                        .Where(p => p.sponsor != null && p.sponsor.Contains(s.SponsorAd) && p.Maliyet.HasValue)
                        .Sum(p => p.Maliyet!.Value)
                })
                .OrderByDescending(x => x.toplamProje)
                .ToList();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        toplamSponsor = sponsorlar.Count,
                        sponsorIstatistikleri = statistics
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchSponsors([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrEmpty(query))
                {
                    return BadRequest(new { success = false, message = "Arama terimi gereklidir" });
                }

                var sponsorlar = await _context.Sponsorler
                    .Where(s => s.SponsorAd.Contains(query) || 
                               (s.IletisimBilgisi != null && s.IletisimBilgisi.Contains(query)) ||
                               (s.Aciklama != null && s.Aciklama.Contains(query)))
                    .OrderBy(s => s.SponsorAd)
                    .Select(s => new
                    {
                        id = s.id,
                        sponsorAd = s.SponsorAd,
                        iletisimBilgisi = s.IletisimBilgisi,
                        aciklama = s.Aciklama
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = sponsorlar,
                    count = sponsorlar.Count,
                    query = query
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }
    }

    public class CreateSponsorRequest
    {
        public string SponsorAd { get; set; } = string.Empty;
        public string? IletisimBilgisi { get; set; }
        public string? Aciklama { get; set; }
    }

    public class UpdateSponsorRequest
    {
        public string SponsorAd { get; set; } = string.Empty;
        public string? IletisimBilgisi { get; set; }
        public string? Aciklama { get; set; }
    }
}