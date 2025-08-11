using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Models;
using ProjeTakip.Services;

namespace ProjeTakip.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProgressController : ControllerBase
    {
        private readonly ProjeTakipContext _context;
        private readonly SystemLogService _systemLogService;

        public ProgressController(ProjeTakipContext context, SystemLogService systemLogService)
        {
            _context = context;
            _systemLogService = systemLogService;
        }

        [HttpGet("project/{projeId}")]
        public async Task<IActionResult> GetProjectProgress(int projeId)
        {
            try
            {
                // Proje kontrolü
                var proje = await _context.Projeler.FindAsync(projeId);
                if (proje == null)
                {
                    return NotFound(new { success = false, message = "Proje bulunamadı" });
                }

                // Gantt aşamalarını getir
                var ganttAsamalari = await _context.GanttAsamalari
                    .Where(g => g.ProjeID == projeId)
                    .OrderBy(g => g.Sira)
                    .Select(g => new
                    {
                        id = g.id,
                        asama = g.Asama,
                        baslangic = g.Baslangic,
                        bitis = g.Bitis,
                        gun = g.Gun,
                        sira = g.Sira,
                        tamamlandi = g.Bitis.HasValue && g.Bitis.Value <= DateTime.Now
                    })
                    .ToListAsync();

                // İlerlemeleri getir
                var ilerlemeler = await _context.Ilerlemeler
                    .Where(i => i.ProjeID == projeId)
                    .Include(i => i.EkleyenKullanici)
                    .Include(i => i.GanttAsama)
                    .OrderByDescending(i => i.IlerlemeTarihi)
                    .Select(i => new
                    {
                        id = i.id,
                        ganttID = i.GanttID,
                        ganttAsama = i.GanttAsama != null ? i.GanttAsama.Asama : "Bilinmiyor",
                        ilerlemeTanimi = i.IlerlemeTanimi,
                        tamamlanmaYuzdesi = i.TamamlanmaYuzdesi,
                        ilerlemeTarihi = i.IlerlemeTarihi,
                        aciklama = i.Aciklama,
                        kullaniciID = i.KullaniciID,
                        ekleyenKullanici = i.EkleyenKullanici != null ? i.EkleyenKullanici.AdSoyad : "Bilinmiyor"
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        proje = new
                        {
                            id = proje.ProjeID,
                            projeAd = proje.ProjeAd,
                            durum = proje.Durum,
                            durumText = GetDurumText(proje.Durum)
                        },
                        ganttAsamalari = ganttAsamalari,
                        ilerlemeler = ilerlemeler
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpPost("add-progress")]
        public async Task<IActionResult> AddProgress([FromBody] AddProgressRequest request)
        {
            try
            {
                // Validasyon
                if (request.ProjeID <= 0 || request.GanttID <= 0)
                {
                    return BadRequest(new { success = false, message = "Geçerli proje ve aşama ID'si gereklidir" });
                }

                if (string.IsNullOrEmpty(request.IlerlemeTanimi))
                {
                    return BadRequest(new { success = false, message = "İlerleme tanımı gereklidir" });
                }

                if (request.TamamlanmaYuzdesi < 0 || request.TamamlanmaYuzdesi > 100)
                {
                    return BadRequest(new { success = false, message = "Tamamlanma yüzdesi 0-100 arasında olmalıdır" });
                }

                // Proje ve aşama kontrolü
                var proje = await _context.Projeler.FindAsync(request.ProjeID);
                if (proje == null)
                {
                    return NotFound(new { success = false, message = "Proje bulunamadı" });
                }

                var ganttAsama = await _context.GanttAsamalari.FindAsync(request.GanttID);
                if (ganttAsama == null)
                {
                    return NotFound(new { success = false, message = "Aşama bulunamadı" });
                }

                // İlerleme kaydı oluştur
                var ilerleme = new Ilerleme
                {
                    ProjeID = request.ProjeID,
                    GanttID = request.GanttID,
                    IlerlemeTanimi = request.IlerlemeTanimi,
                    TamamlanmaYuzdesi = request.TamamlanmaYuzdesi,
                    IlerlemeTarihi = DateTime.Now,
                    Aciklama = request.Aciklama,
                    KullaniciID = request.KullaniciID
                };

                _context.Ilerlemeler.Add(ilerleme);
                await _context.SaveChangesAsync();

                // Log kaydı
                var kullanici = request.KullaniciID.HasValue ? 
                    await _context.Kullanicilar.FindAsync(request.KullaniciID.Value) : null;
                
                await _systemLogService.LogAsync(
                    "İlerleme Eklendi",
                    $"Proje: {proje.ProjeAd}, Aşama: {ganttAsama.Asama}, İlerleme: {request.IlerlemeTanimi}",
                    kullanici?.AdSoyad ?? "System",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                );

                return Ok(new
                {
                    success = true,
                    message = "İlerleme başarıyla eklendi",
                    data = new { id = ilerleme.id }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpPut("update-progress/{id}")]
        public async Task<IActionResult> UpdateProgress(int id, [FromBody] UpdateProgressRequest request)
        {
            try
            {
                var ilerleme = await _context.Ilerlemeler
                    .Include(i => i.Proje)
                    .Include(i => i.GanttAsama)
                    .FirstOrDefaultAsync(i => i.id == id);

                if (ilerleme == null)
                {
                    return NotFound(new { success = false, message = "İlerleme kaydı bulunamadı" });
                }

                // Güncelleme
                if (!string.IsNullOrEmpty(request.IlerlemeTanimi))
                    ilerleme.IlerlemeTanimi = request.IlerlemeTanimi;
                
                if (request.TamamlanmaYuzdesi.HasValue)
                {
                    if (request.TamamlanmaYuzdesi < 0 || request.TamamlanmaYuzdesi > 100)
                    {
                        return BadRequest(new { success = false, message = "Tamamlanma yüzdesi 0-100 arasında olmalıdır" });
                    }
                    ilerleme.TamamlanmaYuzdesi = request.TamamlanmaYuzdesi.Value;
                }

                if (request.Aciklama != null)
                    ilerleme.Aciklama = request.Aciklama;

                await _context.SaveChangesAsync();

                // Log kaydı
                await _systemLogService.LogAsync(
                    "İlerleme Güncellendi",
                    $"Proje: {ilerleme.Proje?.ProjeAd}, Aşama: {ilerleme.GanttAsama?.Asama}, İlerleme: {ilerleme.IlerlemeTanimi}",
                    "System",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                );

                return Ok(new { success = true, message = "İlerleme başarıyla güncellendi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpDelete("delete-progress/{id}")]
        public async Task<IActionResult> DeleteProgress(int id)
        {
            try
            {
                var ilerleme = await _context.Ilerlemeler
                    .Include(i => i.Proje)
                    .Include(i => i.GanttAsama)
                    .FirstOrDefaultAsync(i => i.id == id);

                if (ilerleme == null)
                {
                    return NotFound(new { success = false, message = "İlerleme kaydı bulunamadı" });
                }

                var projeAd = ilerleme.Proje?.ProjeAd ?? "Bilinmiyor";
                var asamaAd = ilerleme.GanttAsama?.Asama ?? "Bilinmiyor";
                var ilerlemeTanimi = ilerleme.IlerlemeTanimi;

                _context.Ilerlemeler.Remove(ilerleme);
                await _context.SaveChangesAsync();

                // Log kaydı
                await _systemLogService.LogAsync(
                    "İlerleme Silindi",
                    $"Proje: {projeAd}, Aşama: {asamaAd}, İlerleme: {ilerlemeTanimi}",
                    "System",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                );

                return Ok(new { success = true, message = "İlerleme başarıyla silindi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpPost("add-gantt-stage")]
        public async Task<IActionResult> AddGanttStage([FromBody] AddGanttStageRequest request)
        {
            try
            {
                if (request.ProjeID <= 0 || string.IsNullOrEmpty(request.Asama))
                {
                    return BadRequest(new { success = false, message = "Proje ID ve aşama adı gereklidir" });
                }

                var proje = await _context.Projeler.FindAsync(request.ProjeID);
                if (proje == null)
                {
                    return NotFound(new { success = false, message = "Proje bulunamadı" });
                }

                var ganttAsama = new Gantt
                {
                    ProjeID = request.ProjeID,
                    Asama = request.Asama,
                    Baslangic = request.Baslangic,
                    Bitis = request.Bitis,
                    Gun = request.Gun ?? 0,
                    Sira = request.Sira ?? 1
                };

                _context.GanttAsamalari.Add(ganttAsama);
                await _context.SaveChangesAsync();

                // Log kaydı
                await _systemLogService.LogAsync(
                    "Gantt Aşaması Eklendi",
                    $"Proje: {proje.ProjeAd}, Aşama: {request.Asama}",
                    "System",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                );

                return Ok(new
                {
                    success = true,
                    message = "Aşama başarıyla eklendi",
                    data = new { id = ganttAsama.id }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        private static string GetDurumText(int durum)
        {
            return durum switch
            {
                1 => "Onay Bekliyor",
                2 => "Planlama",
                3 => "Devam Ediyor",
                4 => "Tamamlandı",
                5 => "İptal Edildi",
                _ => "Bilinmiyor"
            };
        }
    }

    public class AddProgressRequest
    {
        public int ProjeID { get; set; }
        public int GanttID { get; set; }
        public string IlerlemeTanimi { get; set; } = string.Empty;
        public int TamamlanmaYuzdesi { get; set; }
        public string? Aciklama { get; set; }
        public int? KullaniciID { get; set; }
    }

    public class UpdateProgressRequest
    {
        public string? IlerlemeTanimi { get; set; }
        public int? TamamlanmaYuzdesi { get; set; }
        public string? Aciklama { get; set; }
    }

    public class AddGanttStageRequest
    {
        public int ProjeID { get; set; }
        public string Asama { get; set; } = string.Empty;
        public DateTime? Baslangic { get; set; }
        public DateTime? Bitis { get; set; }
        public int? Gun { get; set; }
        public int? Sira { get; set; }
    }
}