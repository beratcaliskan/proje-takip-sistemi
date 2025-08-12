using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Models;
using ProjeTakip.Services;

namespace ProjeTakip.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly ProjeTakipContext _context;
        private readonly SystemLogService _systemLogService;

        public ProjectsController(ProjeTakipContext context, SystemLogService systemLogService)
        {
            _context = context;
            _systemLogService = systemLogService;
        }

        private async Task<string> GetCurrentUserNameAsync()
        {
            try
            {
                var userId = ExtractUserIdFromToken();
                if (userId <= 0)
                {
                    return "System";
                }

                var kullanici = await _context.Kullanicilar.FindAsync(userId);
                
                return kullanici?.AdSoyad ?? "System";
            }
            catch
            {
                return "System";
            }
        }

        private int ExtractUserIdFromToken()
        {
            try
            {
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != null && authHeader.StartsWith("Bearer "))
                {
                    var token = authHeader.Substring("Bearer ".Length).Trim();
                    var bytes = Convert.FromBase64String(token);
                    return BitConverter.ToInt32(bytes, 0);
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProjects([FromQuery] int? durum, [FromQuery] string? mudurluk)
        {
            try
            {
                var query = _context.Projeler.AsQueryable();

                // Durum filtresi
                if (durum.HasValue)
                {
                    query = query.Where(p => p.Durum == durum.Value);
                }

                // Müdürlük filtresi
                if (!string.IsNullOrEmpty(mudurluk))
                {
                    query = query.Where(p => p.Mudurluk.Contains(mudurluk));
                }

                var projeler = await query
                    .OrderByDescending(p => p.ProjeID)
                    .Select(p => new
                    {
                        id = p.ProjeID,
                        projeAd = p.ProjeAd,
                        mudurluk = p.Mudurluk,
                        baskanlik = p.Baskanlik,
                        durum = p.Durum,
                        durumText = GetDurumText(p.Durum),
                        baslangicTarihi = p.BaslangicTarihi,
                        bitisTarihi = p.BitisTarihi,
                        maliyet = p.Maliyet,
                        maliyetFormatli = p.MaliyetFormatli,
                        amac = p.Amac,
                        kapsam = p.Kapsam,
                        ekip = p.Ekip,
                        personel = p.personel,
                        sponsor = p.sponsor,
                        olcut = p.olcut
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = projeler,
                    count = projeler.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProject(int id)
        {
            try
            {
                var proje = await _context.Projeler
                    .FirstOrDefaultAsync(p => p.ProjeID == id);

                if (proje == null)
                {
                    return NotFound(new { success = false, message = "Proje bulunamadı" });
                }

                // Proje aşamalarını getir
                var asamalar = await _context.GanttAsamalari
                    .Where(g => g.ProjeID == id)
                    .OrderBy(g => g.Sira)
                    .Select(g => new
                    {
                        id = g.id,
                        asama = g.Asama,
                        baslangic = g.Baslangic,
                        bitis = g.Bitis,
                        gun = g.Gun,
                        sira = g.Sira
                    })
                    .ToListAsync();

                // Proje ilerlemelerini getir
                var ilerlemeler = await _context.Ilerlemeler
                    .Where(i => i.ProjeID == id)
                    .OrderByDescending(i => i.IlerlemeTarihi)
                    .Select(i => new
                    {
                        id = i.id,
                        ilerlemeTanimi = i.IlerlemeTanimi,
                        tamamlanmaYuzdesi = i.TamamlanmaYuzdesi,
                        ilerlemeTarihi = i.IlerlemeTarihi,
                        aciklama = i.Aciklama,
                        kullaniciID = i.KullaniciID
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
                            mudurluk = proje.Mudurluk,
                            baskanlik = proje.Baskanlik,
                            durum = proje.Durum,
                            durumText = GetDurumText(proje.Durum),
                            baslangicTarihi = proje.BaslangicTarihi,
                            bitisTarihi = proje.BitisTarihi,
                            maliyet = proje.Maliyet,
                            maliyetFormatli = proje.MaliyetFormatli,
                            amac = proje.Amac,
                            kapsam = proje.Kapsam,
                            ekip = proje.Ekip,
                            personel = proje.personel,
                            sponsor = proje.sponsor,
                            olcut = proje.olcut
                        },
                        asamalar = asamalar,
                        ilerlemeler = ilerlemeler
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ProjeAd))
                {
                    return BadRequest(new { success = false, message = "Proje adı gereklidir" });
                }

                var proje = new Proje
                {
                    ProjeAd = request.ProjeAd,
                    Mudurluk = request.Mudurluk ?? string.Empty,
                    Baskanlik = request.Baskanlik ?? string.Empty,
                    Durum = request.Durum ?? 1,
                    bas = request.BaslangicTarihi,
                    bit = request.BitisTarihi,
                    Maliyet = request.Maliyet,
                    Amac = request.Amac,
                    Kapsam = request.Kapsam,
                    Ekip = request.Ekip,
                    personel = request.Personel ?? 0,
                    sponsor = request.Sponsor,
                    olcut = request.Olcut
                };

                _context.Projeler.Add(proje);
                await _context.SaveChangesAsync();

                // Token'dan kullanıcı bilgisini al
                var kullaniciAdi = await GetCurrentUserNameAsync();

                // Log kaydı
                await _systemLogService.LogAsync(
                    "Proje Eklendi",
                    $"Yeni proje eklendi: {proje.ProjeAd}",
                    kullaniciAdi,
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                );

                return Ok(new
                {
                    success = true,
                    message = "Proje başarıyla oluşturuldu",
                    data = new { id = proje.ProjeID }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProject(int id, [FromBody] UpdateProjectRequest request)
        {
            try
            {
                var proje = await _context.Projeler.FindAsync(id);
                if (proje == null)
                {
                    return NotFound(new { success = false, message = "Proje bulunamadı" });
                }

                // Güncelleme
                if (!string.IsNullOrEmpty(request.ProjeAd))
                    proje.ProjeAd = request.ProjeAd;
                if (!string.IsNullOrEmpty(request.Mudurluk))
                    proje.Mudurluk = request.Mudurluk;
                if (!string.IsNullOrEmpty(request.Baskanlik))
                    proje.Baskanlik = request.Baskanlik;
                if (request.Durum.HasValue)
                    proje.Durum = request.Durum.Value;
                if (request.BaslangicTarihi.HasValue)
                    proje.bas = request.BaslangicTarihi;
                if (request.BitisTarihi.HasValue)
                    proje.bit = request.BitisTarihi;
                if (request.Maliyet.HasValue)
                    proje.Maliyet = request.Maliyet;
                if (request.Amac != null)
                    proje.Amac = request.Amac;
                if (request.Kapsam != null)
                    proje.Kapsam = request.Kapsam;
                if (request.Ekip != null)
                    proje.Ekip = request.Ekip;
                if (request.Personel.HasValue)
                    proje.personel = request.Personel.Value;
                if (request.Sponsor != null)
                    proje.sponsor = request.Sponsor;
                if (request.Olcut != null)
                    proje.olcut = request.Olcut;

                await _context.SaveChangesAsync();

                // Token'dan kullanıcı bilgisini al
                var kullaniciAdi = await GetCurrentUserNameAsync();

                // Log kaydı
                await _systemLogService.LogAsync(
                    "Proje Güncellendi",
                    $"Proje güncellendi: {proje.ProjeAd}",
                    kullaniciAdi,
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                );

                return Ok(new { success = true, message = "Proje başarıyla güncellendi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            try
            {
                var proje = await _context.Projeler.FindAsync(id);
                if (proje == null)
                {
                    return NotFound(new { success = false, message = "Proje bulunamadı" });
                }

                var projeAd = proje.ProjeAd;
                _context.Projeler.Remove(proje);
                await _context.SaveChangesAsync();

                // Token'dan kullanıcı bilgisini al
                var kullaniciAdi = await GetCurrentUserNameAsync();

                // Log kaydı
                await _systemLogService.LogAsync(
                    "Proje Silindi",
                    $"Proje silindi: {projeAd}",
                    kullaniciAdi,
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                );

                return Ok(new { success = true, message = "Proje başarıyla silindi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetProjectStatistics()
        {
            try
            {
                var projeler = await _context.Projeler.ToListAsync();

                var statistics = new
                {
                    toplamProje = projeler.Count,
                    aktifProje = projeler.Count(p => p.Durum == 2 || p.Durum == 3),
                    tamamlananProje = projeler.Count(p => p.Durum == 4),
                    iptalEdilenProje = projeler.Count(p => p.Durum == 5),
                    onayBekleyenProje = projeler.Count(p => p.Durum == 1),
                    toplamMaliyet = projeler.Where(p => p.Maliyet.HasValue).Sum(p => p.Maliyet!.Value),
                    ortalamaMaliyet = projeler.Where(p => p.Maliyet.HasValue).Any() ? 
                        projeler.Where(p => p.Maliyet.HasValue).Average(p => p.Maliyet!.Value) : 0
                };

                return Ok(new { success = true, data = statistics });
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

    public class CreateProjectRequest
    {
        public string ProjeAd { get; set; } = string.Empty;
        public string? Mudurluk { get; set; }
        public string? Baskanlik { get; set; }
        public int? Durum { get; set; }
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime? BitisTarihi { get; set; }
        public decimal? Maliyet { get; set; }
        public string? Amac { get; set; }
        public string? Kapsam { get; set; }
        public string? Ekip { get; set; }
        public int? Personel { get; set; }
        public string? Sponsor { get; set; }
        public string? Olcut { get; set; }
    }

    public class UpdateProjectRequest
    {
        public string? ProjeAd { get; set; }
        public string? Mudurluk { get; set; }
        public string? Baskanlik { get; set; }
        public int? Durum { get; set; }
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime? BitisTarihi { get; set; }
        public decimal? Maliyet { get; set; }
        public string? Amac { get; set; }
        public string? Kapsam { get; set; }
        public string? Ekip { get; set; }
        public int? Personel { get; set; }
        public string? Sponsor { get; set; }
        public string? Olcut { get; set; }
    }
}