using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace ProjeTakip.Middleware
{
    public class SessionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string[] _excludedPaths = { "/login", "/error", "/css", "/js", "/lib", "/favicon.ico", "/api" };

        public SessionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";
            
            // Excluded paths'leri kontrol et
            bool isExcluded = false;
            foreach (var excludedPath in _excludedPaths)
            {
                if (path.StartsWith(excludedPath))
                {
                    isExcluded = true;
                    break;
                }
            }

            // Eğer excluded path değilse ve session yoksa login'e yönlendir
            if (!isExcluded && string.IsNullOrEmpty(context.Session.GetString("UserId")))
            {
                context.Response.Redirect("/Login");
                return;
            }

            await _next(context);
        }
    }
}