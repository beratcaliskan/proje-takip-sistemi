using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace ProjeTakip.Middleware
{
    public class ApiAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string[] _publicEndpoints = { 
            "/api/auth/login", 
            "/api/auth/validate-token",
            "/swagger",
            "/api-docs"
        };

        public ApiAuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";
            
            // API endpoint'i değilse middleware'i atla
            if (!path.StartsWith("/api"))
            {
                await _next(context);
                return;
            }

            // Public endpoint'leri kontrol et
            bool isPublicEndpoint = false;
            foreach (var publicEndpoint in _publicEndpoints)
            {
                if (path.StartsWith(publicEndpoint.ToLower()))
                {
                    isPublicEndpoint = true;
                    break;
                }
            }

            // Public endpoint ise authentication gerektirmez
            if (isPublicEndpoint)
            {
                await _next(context);
                return;
            }

            // Authorization header kontrolü
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("{\"success\": false, \"message\": \"Authorization token gereklidir\"}");
                return;
            }

            // Token'ı çıkar
            var token = authHeader.Substring("Bearer ".Length).Trim();
            
            // Token validasyonu - gerçek token kontrolü
            if (!await IsValidTokenAsync(token, context))
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"success\": false, \"message\": \"Geçersiz veya süresi dolmuş token\"}");
                return;
            }

            // Token geçerli, devam et
            await _next(context);
        }

        private async Task<bool> IsValidTokenAsync(string token, HttpContext context)
        {
            try
            {
                // Token format kontrolü
                if (string.IsNullOrEmpty(token) || token.Length < 20)
                    return false;

                // Base64 decode kontrolü
                try
                {
                    var bytes = Convert.FromBase64String(token);
                    if (bytes.Length < 16) // En az 16 byte olmalı
                        return false;
                }
                catch
                {
                    return false;
                }

                // Veritabanından kullanıcı kontrolü (basit implementasyon)
                // Gerçek uygulamada JWT decode edilmeli ve claims kontrol edilmeli
                var userId = ExtractUserIdFromToken(token);
                if (userId <= 0)
                    return false;

                // Kullanıcının var olup olmadığını kontrol et
                var dbContextService = context.RequestServices.GetService<ProjeTakip.Data.ProjeTakipContext>();
                if (dbContextService != null)
                {
                    var user = await dbContextService.Kullanicilar.FindAsync(userId);
                    return user != null;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private int ExtractUserIdFromToken(string token)
        {
            try
            {
                var bytes = Convert.FromBase64String(token);
                // İlk 4 byte'ı user ID olarak kullan
                if (bytes.Length >= 4)
                {
                    var userId = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 1000 + 1;
                    return userId;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}