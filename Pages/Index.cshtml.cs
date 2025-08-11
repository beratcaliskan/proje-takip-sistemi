using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjeTakip.Data;
using Microsoft.EntityFrameworkCore;

namespace ProjeTakip.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ProjeTakipContext _context;

        public IndexModel(ILogger<IndexModel> logger, ProjeTakipContext context)
        {
            _logger = logger;
            _context = context;
        }

        public string UserName { get; set; } = string.Empty;
        public string UserKimlik { get; set; } = string.Empty;
        public int UserRole { get; set; }
        
        // Dashboard istatistikleri
        public int ToplamProjeler { get; set; }
        public int AktifProjeler { get; set; }
        public int TamamlananProjeler { get; set; }
        public int BekleyenProjeler { get; set; }

        public async Task<IActionResult> OnGetAsync()
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
            
            // Dashboard istatistiklerini hesapla
            try
            {
                ToplamProjeler = await _context.Projeler.CountAsync();
                AktifProjeler = await _context.Projeler.CountAsync(p => p.Durum == 3); // Devam Ediyor
                TamamlananProjeler = await _context.Projeler.CountAsync(p => p.Durum == 4); // Tamamlandı
                BekleyenProjeler = await _context.Projeler.CountAsync(p => p.Durum == 1); // Onay Bekliyor
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard istatistikleri yüklenirken hata oluştu");
                // Hata durumunda varsayılan değerler
                ToplamProjeler = 0;
                AktifProjeler = 0;
                TamamlananProjeler = 0;
                BekleyenProjeler = 0;
            }

            return Page();
        }
    }
}
