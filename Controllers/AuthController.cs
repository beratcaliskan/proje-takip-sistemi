using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Models;
using ProjeTakip.Services;
using System.Security.Cryptography;
using System.Text;

namespace ProjeTakip.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ProjeTakipContext _context;
        private readonly SystemLogService _systemLogService;

        public AuthController(ProjeTakipContext context, SystemLogService systemLogService)
        {
            _context = context;
            _systemLogService = systemLogService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Kimlik) || string.IsNullOrEmpty(request.Sifre))
                {
                    return BadRequest(new { success = false, message = "Kimlik ve şifre gereklidir" });
                }

                // Kullanıcıyı bul
                var kullanici = await _context.Kullanicilar
                    .FirstOrDefaultAsync(k => k.Kimlik == request.Kimlik);

                if (kullanici == null)
                {
                    return Unauthorized(new { success = false, message = "Geçersiz kimlik veya şifre" });
                }

                // Şifre kontrolü
                if (!VerifyPassword(request.Sifre, kullanici.Sifre))
                {
                    return Unauthorized(new { success = false, message = "Geçersiz kimlik veya şifre" });
                }

                // Session token oluştur
                var sessionToken = GenerateSessionToken(kullanici.id);
                var expiryTime = DateTime.UtcNow.AddHours(24); // 24 saat geçerli

                // Session bilgisini veritabanına kaydet (isteğe bağlı)
                // Burada bir UserSessions tablosu oluşturabilirsiniz

                // Giriş logunu kaydet
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var userAgent = Request.Headers["User-Agent"].ToString();
                
                await _systemLogService.LogUserLoginAsync(
                    kullanici.Kimlik, 
                    kullanici.AdSoyad, 
                    ipAddress, 
                    userAgent
                );

                return Ok(new
                {
                    success = true,
                    message = "Giriş başarılı",
                    data = new
                    {
                        sessionToken = sessionToken,
                        expiryTime = expiryTime,
                        user = new
                        {
                            id = kullanici.id,
                            adSoyad = kullanici.AdSoyad,
                            kimlik = kullanici.Kimlik,
                            rol = kullanici.Rol
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromHeader] string Authorization)
        {
            try
            {
                // Authorization header'dan token'ı al
                var token = ExtractTokenFromHeader(Authorization);
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { success = false, message = "Geçersiz token" });
                }

                // Token'dan kullanıcı bilgisini al (basit implementasyon)
                // Gerçek uygulamada JWT veya database session kullanılmalı
                var userId = ExtractUserIdFromToken(token);
                if (userId > 0)
                {
                    var kullanici = await _context.Kullanicilar.FindAsync(userId);
                    if (kullanici != null)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                        var userAgent = Request.Headers["User-Agent"].ToString();
                        
                        await _systemLogService.LogUserLogoutAsync(
                            kullanici.Kimlik, 
                            kullanici.AdSoyad, 
                            ipAddress
                        );
                    }
                }

                // Session'ı sonlandır (database'den sil)
                // InvalidateSession(token);

                return Ok(new { success = true, message = "Çıkış başarılı" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpGet("validate-token")]
        public async Task<IActionResult> ValidateToken([FromHeader] string Authorization)
        {
            try
            {
                var token = ExtractTokenFromHeader(Authorization);
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { success = false, message = "Token gereklidir" });
                }

                // Token geçerliliğini kontrol et
                var userId = ExtractUserIdFromToken(token);
                if (userId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Geçersiz token" });
                }

                var kullanici = await _context.Kullanicilar.FindAsync(userId);
                if (kullanici == null)
                {
                    return Unauthorized(new { success = false, message = "Kullanıcı bulunamadı" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        user = new
                        {
                            id = kullanici.id,
                            adSoyad = kullanici.AdSoyad,
                            kimlik = kullanici.Kimlik,
                            rol = kullanici.Rol
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        private bool VerifyPassword(string inputPassword, string storedPassword)
        {
            // Basit string karşılaştırması (gerçek uygulamada hash kullanılmalı)
            return inputPassword == storedPassword;
        }

        private string GenerateSessionToken(int userId)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[32];
                rng.GetBytes(bytes);
                
                // İlk 4 byte'a user ID'yi yerleştir
                var userIdBytes = BitConverter.GetBytes(userId);
                Array.Copy(userIdBytes, 0, bytes, 0, 4);
                
                return Convert.ToBase64String(bytes);
            }
        }

        private string ExtractTokenFromHeader(string authHeader)
        {
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return string.Empty;
            
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        private int ExtractUserIdFromToken(string token)
        {
            // Basit implementasyon - gerçek uygulamada JWT decode edilmeli
            // Şimdilik token'ın ilk 8 karakterini user ID olarak kullanıyoruz
            try
            {
                var bytes = Convert.FromBase64String(token);
                return Math.Abs(BitConverter.ToInt32(bytes, 0)) % 1000 + 1; // 1-1000 arası ID
            }
            catch
            {
                return 0;
            }
        }
    }

    public class LoginRequest
    {
        public string Kimlik { get; set; } = string.Empty;
        public string Sifre { get; set; } = string.Empty;
    }
}