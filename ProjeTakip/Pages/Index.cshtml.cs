using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjeTakip.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public string UserName { get; set; } = string.Empty;
        public string UserKimlik { get; set; } = string.Empty;
        public int UserRole { get; set; }

        public IActionResult OnGet()
        {
            // Giriş kontrolü
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToPage("/Login");
            }

            // Kullanıcı bilgilerini session'dan al
            UserName = HttpContext.Session.GetString("UserName") ?? "";
            UserKimlik = HttpContext.Session.GetString("UserKimlik") ?? "";
            UserRole = HttpContext.Session.GetInt32("UserRole") ?? 0;

            return Page();
        }
    }
}
