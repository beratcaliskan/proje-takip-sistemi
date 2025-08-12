using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Models;
using ProjeTakip.Services;

namespace ProjeTakip.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ProjeTakipContext _context;
        private readonly SystemLogService _systemLogService;

        public UsersController(ProjeTakipContext context, SystemLogService systemLogService)
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
        public async Task<IActionResult> GetUsers([FromQuery] int? rol)
        {
            try
            {
                var query = _context.Kullanicilar.AsQueryable();

                // Rol filtresi
                if (rol.HasValue)
                {
                    query = query.Where(k => k.Rol == rol.Value);
                }

                var kullanicilar = await query
                    .OrderBy(k => k.AdSoyad)
                    .Select(k => new
                    {
                        id = k.id,
                        adSoyad = k.AdSoyad,
                        kimlik = k.Kimlik,
                        rol = k.Rol,
                        rolText = GetRolText(k.Rol)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = kullanicilar,
                    count = kullanicilar.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var kullanici = await _context.Kullanicilar.FindAsync(id);
                if (kullanici == null)
                {
                    return NotFound(new { success = false, message = "Kullanıcı bulunamadı" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = kullanici.id,
                        adSoyad = kullanici.AdSoyad,
                        kimlik = kullanici.Kimlik,
                        rol = kullanici.Rol,
                        rolText = GetRolText(kullanici.Rol)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                // Validasyon
                if (string.IsNullOrEmpty(request.AdSoyad) || string.IsNullOrEmpty(request.Kimlik))
                {
                    return BadRequest(new { success = false, message = "Ad Soyad ve Kimlik gereklidir" });
                }

                if (string.IsNullOrEmpty(request.Sifre))
                {
                    return BadRequest(new { success = false, message = "Şifre gereklidir" });
                }

                // Kimlik kontrolü (benzersiz olmalı)
                var mevcutKullanici = await _context.Kullanicilar
                    .FirstOrDefaultAsync(k => k.Kimlik == request.Kimlik);
                
                if (mevcutKullanici != null)
                {
                    return BadRequest(new { success = false, message = "Bu kimlik zaten kullanılıyor" });
                }

                var kullanici = new Kullanici
                {
                    AdSoyad = request.AdSoyad,
                    Kimlik = request.Kimlik,
                    Sifre = request.Sifre, // Gerçek uygulamada hash'lenmeli
                    Rol = request.Rol ?? 3 // Varsayılan rol: 3 (Normal kullanıcı)
                };

                _context.Kullanicilar.Add(kullanici);
                await _context.SaveChangesAsync();

                // Token'dan kullanıcı bilgisini al
                var kullaniciAdi = await GetCurrentUserNameAsync();

                // Log kaydı
                await _systemLogService.LogAsync(
                    "Kullanıcı Eklendi",
                    $"Yeni kullanıcı eklendi: {kullanici.AdSoyad} ({kullanici.Kimlik})",
                    kullaniciAdi,
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                );

                return Ok(new
                {
                    success = true,
                    message = "Kullanıcı başarıyla oluşturuldu",
                    data = new { id = kullanici.id }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var kullanici = await _context.Kullanicilar.FindAsync(id);
                if (kullanici == null)
                {
                    return NotFound(new { success = false, message = "Kullanıcı bulunamadı" });
                }

                // Kimlik değişikliği kontrolü
                if (!string.IsNullOrEmpty(request.Kimlik) && request.Kimlik != kullanici.Kimlik)
                {
                    var mevcutKullanici = await _context.Kullanicilar
                        .FirstOrDefaultAsync(k => k.Kimlik == request.Kimlik && k.id != id);
                    
                    if (mevcutKullanici != null)
                    {
                        return BadRequest(new { success = false, message = "Bu kimlik zaten kullanılıyor" });
                    }
                    kullanici.Kimlik = request.Kimlik;
                }

                // Güncelleme
                if (!string.IsNullOrEmpty(request.AdSoyad))
                    kullanici.AdSoyad = request.AdSoyad;
                
                if (!string.IsNullOrEmpty(request.Sifre))
                    kullanici.Sifre = request.Sifre; // Gerçek uygulamada hash'lenmeli
                
                if (request.Rol.HasValue)
                    kullanici.Rol = request.Rol.Value;

                await _context.SaveChangesAsync();

                // Token'dan kullanıcı bilgisini al
                var kullaniciAdi = await GetCurrentUserNameAsync();

                // Log kaydı
                await _systemLogService.LogAsync(
                    "Kullanıcı Güncellendi",
                    $"Kullanıcı güncellendi: {kullanici.AdSoyad} ({kullanici.Kimlik})",
                    kullaniciAdi,
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                );

                return Ok(new { success = true, message = "Kullanıcı başarıyla güncellendi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var kullanici = await _context.Kullanicilar.FindAsync(id);
                if (kullanici == null)
                {
                    return NotFound(new { success = false, message = "Kullanıcı bulunamadı" });
                }

                var adSoyad = kullanici.AdSoyad;
                var kimlik = kullanici.Kimlik;

                _context.Kullanicilar.Remove(kullanici);
                await _context.SaveChangesAsync();

                // Token'dan kullanıcı bilgisini al
                var kullaniciAdi = await GetCurrentUserNameAsync();

                // Log kaydı
                await _systemLogService.LogAsync(
                    "Kullanıcı Silindi",
                    $"Kullanıcı silindi: {adSoyad} ({kimlik})",
                    kullaniciAdi,
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                );

                return Ok(new { success = true, message = "Kullanıcı başarıyla silindi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetUserStatistics()
        {
            try
            {
                var kullanicilar = await _context.Kullanicilar.ToListAsync();

                var statistics = new
                {
                    toplamKullanici = kullanicilar.Count,
                    adminKullanici = kullanicilar.Count(k => k.Rol == 1),
                    moderatorKullanici = kullanicilar.Count(k => k.Rol == 2),
                    normalKullanici = kullanicilar.Count(k => k.Rol == 3),
                    rolDagilimi = kullanicilar
                        .GroupBy(k => k.Rol)
                        .Select(g => new
                        {
                            rol = g.Key,
                            rolText = GetRolText(g.Key),
                            sayi = g.Count()
                        })
                        .OrderBy(x => x.rol)
                        .ToList()
                };

                return Ok(new { success = true, data = statistics });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (request.UserId <= 0 || string.IsNullOrEmpty(request.OldPassword) || string.IsNullOrEmpty(request.NewPassword))
                {
                    return BadRequest(new { success = false, message = "Tüm alanlar gereklidir" });
                }

                var kullanici = await _context.Kullanicilar.FindAsync(request.UserId);
                if (kullanici == null)
                {
                    return NotFound(new { success = false, message = "Kullanıcı bulunamadı" });
                }

                // Eski şifre kontrolü
                if (kullanici.Sifre != request.OldPassword)
                {
                    return BadRequest(new { success = false, message = "Eski şifre yanlış" });
                }

                // Yeni şifre güncelleme
                kullanici.Sifre = request.NewPassword; // Gerçek uygulamada hash'lenmeli
                await _context.SaveChangesAsync();

                // Log kaydı
                await _systemLogService.LogAsync(
                    "Şifre Değiştirildi",
                    $"Kullanıcı şifresini değiştirdi: {kullanici.AdSoyad}",
                    kullanici.AdSoyad,
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                );

                return Ok(new { success = true, message = "Şifre başarıyla değiştirildi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        private string GetRolText(int rol)
        {
            return rol switch
            {
                1 => "Admin",
                2 => "Moderatör",
                3 => "Kullanıcı",
                _ => "Bilinmiyor"
            };
        }
    }

    public class CreateUserRequest
    {
        public string AdSoyad { get; set; } = string.Empty;
        public string Kimlik { get; set; } = string.Empty;
        public string Sifre { get; set; } = string.Empty;
        public int? Rol { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? AdSoyad { get; set; }
        public string? Kimlik { get; set; }
        public string? Sifre { get; set; }
        public int? Rol { get; set; }
    }

    public class ChangePasswordRequest
    {
        public int UserId { get; set; }
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}