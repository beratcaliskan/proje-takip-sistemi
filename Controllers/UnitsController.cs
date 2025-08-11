using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Models;
using ProjeTakip.Services;

namespace ProjeTakip.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UnitsController : ControllerBase
    {
        private readonly ProjeTakipContext _context;
        private readonly SystemLogService _systemLogService;

        public UnitsController(ProjeTakipContext context, SystemLogService systemLogService)
        {
            _context = context;
            _systemLogService = systemLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUnits()
        {
            try
            {
                var birimler = await _context.Birimler
                    .OrderBy(b => b.BirimAd)
                    .Select(b => new
                    {
                        id = b.id,
                        birimAd = b.BirimAd
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = birimler,
                    count = birimler.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUnit(int id)
        {
            try
            {
                var birim = await _context.Birimler.FindAsync(id);
                if (birim == null)
                {
                    return NotFound(new { success = false, message = "Birim bulunamadı" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = birim.id,
                        birimAd = birim.BirimAd
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateUnit([FromBody] CreateUnitRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.BirimAd))
                {
                    return BadRequest(new { success = false, message = "Birim adı gereklidir" });
                }

                // Aynı isimde birim kontrolü
                var mevcutBirim = await _context.Birimler
                    .FirstOrDefaultAsync(b => b.BirimAd.ToLower() == request.BirimAd.ToLower());
                
                if (mevcutBirim != null)
                {
                    return BadRequest(new { success = false, message = "Bu birim adı zaten kullanılıyor" });
                }

                var birim = new Birim
                {
                    BirimAd = request.BirimAd
                };

                _context.Birimler.Add(birim);
                await _context.SaveChangesAsync();

                // Log kaydı
                await _systemLogService.LogAsync(
                    "Birim Eklendi",
                    $"Yeni birim eklendi: {birim.BirimAd}",
                    "System",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                );

                return Ok(new
                {
                    success = true,
                    message = "Birim başarıyla oluşturuldu",
                    data = new { id = birim.id }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUnit(int id, [FromBody] UpdateUnitRequest request)
        {
            try
            {
                var birim = await _context.Birimler.FindAsync(id);
                if (birim == null)
                {
                    return NotFound(new { success = false, message = "Birim bulunamadı" });
                }

                if (string.IsNullOrEmpty(request.BirimAd))
                {
                    return BadRequest(new { success = false, message = "Birim adı gereklidir" });
                }

                // Aynı isimde başka birim kontrolü
                var mevcutBirim = await _context.Birimler
                    .FirstOrDefaultAsync(b => b.BirimAd.ToLower() == request.BirimAd.ToLower() && b.id != id);
                
                if (mevcutBirim != null)
                {
                    return BadRequest(new { success = false, message = "Bu birim adı zaten kullanılıyor" });
                }

                var eskiAd = birim.BirimAd;
                birim.BirimAd = request.BirimAd;
                await _context.SaveChangesAsync();

                // Log kaydı
                await _systemLogService.LogAsync(
                    "Birim Güncellendi",
                    $"Birim güncellendi: {eskiAd} -> {birim.BirimAd}",
                    "System",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                );

                return Ok(new { success = true, message = "Birim başarıyla güncellendi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUnit(int id)
        {
            try
            {
                var birim = await _context.Birimler.FindAsync(id);
                if (birim == null)
                {
                    return NotFound(new { success = false, message = "Birim bulunamadı" });
                }

                // Bu birime bağlı projeler var mı kontrol et
                var bagliProjeler = await _context.Projeler
                    .Where(p => p.BirimId == id)
                    .CountAsync();

                if (bagliProjeler > 0)
                {
                    return BadRequest(new 
                    { 
                        success = false, 
                        message = $"Bu birime bağlı {bagliProjeler} adet proje bulunduğu için silinemez" 
                    });
                }

                var birimAd = birim.BirimAd;
                _context.Birimler.Remove(birim);
                await _context.SaveChangesAsync();

                // Log kaydı
                await _systemLogService.LogAsync(
                    "Birim Silindi",
                    $"Birim silindi: {birimAd}",
                    "System",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                );

                return Ok(new { success = true, message = "Birim başarıyla silindi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetUnitStatistics()
        {
            try
            {
                var birimler = await _context.Birimler
                    .Include(b => b.Projeler)
                    .ToListAsync();

                var statistics = birimler.Select(b => new
                {
                    id = b.id,
                    birimAd = b.BirimAd,
                    toplamProje = b.Projeler?.Count ?? 0,
                    aktifProje = b.Projeler?.Count(p => p.Durum == 2 || p.Durum == 3) ?? 0,
                    tamamlananProje = b.Projeler?.Count(p => p.Durum == 4) ?? 0,
                    toplamMaliyet = b.Projeler?.Where(p => p.Maliyet.HasValue).Sum(p => p.Maliyet!.Value) ?? 0
                })
                .OrderByDescending(x => x.toplamProje)
                .ToList();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        toplamBirim = birimler.Count,
                        birimIstatistikleri = statistics
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }
    }

    public class CreateUnitRequest
    {
        public string BirimAd { get; set; } = string.Empty;
    }

    public class UpdateUnitRequest
    {
        public string BirimAd { get; set; } = string.Empty;
    }
}