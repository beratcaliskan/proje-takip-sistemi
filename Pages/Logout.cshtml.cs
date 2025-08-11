using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjeTakip.Services;

namespace ProjeTakip.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly ISystemLogService _logService;

        public LogoutModel(ISystemLogService logService)
        {
            _logService = logService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Çıkış işlemi için log kaydı
            var userName = HttpContext.Session.GetString("UserName");
            var userKimlik = HttpContext.Session.GetString("UserKimlik");
            
            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(userKimlik))
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                await _logService.LogUserLogoutAsync(userKimlik, userName, ipAddress);
            }
            
            // Session'ı temizle (logout işlemi)
            HttpContext.Session.Clear();
            
            return Page();
        }
    }
}