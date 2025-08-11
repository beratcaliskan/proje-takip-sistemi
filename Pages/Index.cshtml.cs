using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjeTakip.Data;
using Microsoft.EntityFrameworkCore;
using ProjeTakip.Models;

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
        
        // Aylık artış oranları
        public double ToplamProjelerArtis { get; set; }
        public double AktifProjelerArtis { get; set; }
        public double TamamlananProjelerArtis { get; set; }
        public double BekleyenProjelerArtis { get; set; }
        
        // Haftalık istatistikler
        public int HaftalikYeniProjeler { get; set; }
        public int HaftalikTamamlananProjeler { get; set; }
        public int HaftalikIlerlemeler { get; set; }
        public int AktifKullanicilar { get; set; }
        
        // Son aktiviteler (login/logout hariç)
        public List<SystemLog> SonAktiviteler { get; set; } = new List<SystemLog>();

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
                var buAy = DateTime.Now;
                var gecenAy = buAy.AddMonths(-1);
                var buHafta = DateTime.Now.AddDays(-7);
                
                // Mevcut ay istatistikleri
                ToplamProjeler = await _context.Projeler.CountAsync();
                AktifProjeler = await _context.Projeler.CountAsync(p => p.Durum == 3); // Devam Ediyor
                TamamlananProjeler = await _context.Projeler.CountAsync(p => p.Durum == 4); // Tamamlandı
                BekleyenProjeler = await _context.Projeler.CountAsync(p => p.Durum == 1); // Onay Bekliyor
                
                // Geçen ay istatistikleri
                var gecenAyToplamProjeler = await _context.Projeler
                    .CountAsync(p => p.bas.HasValue && p.bas < gecenAy.AddMonths(1) && p.bas >= gecenAy);
                var gecenAyAktifProjeler = await _context.Projeler
                    .CountAsync(p => p.Durum == 3 && p.bas.HasValue && p.bas < gecenAy.AddMonths(1) && p.bas >= gecenAy);
                var gecenAyTamamlananProjeler = await _context.Projeler
                    .CountAsync(p => p.Durum == 4 && p.bit.HasValue && p.bit >= gecenAy && p.bit < gecenAy.AddMonths(1));
                var gecenAyBekleyenProjeler = await _context.Projeler
                    .CountAsync(p => p.Durum == 1 && p.bas.HasValue && p.bas < gecenAy.AddMonths(1) && p.bas >= gecenAy);
                
                // Aylık artış oranlarını hesapla
                ToplamProjelerArtis = gecenAyToplamProjeler > 0 ? 
                    Math.Round(((double)(ToplamProjeler - gecenAyToplamProjeler) / gecenAyToplamProjeler) * 100, 1) : 0;
                AktifProjelerArtis = gecenAyAktifProjeler > 0 ? 
                    Math.Round(((double)(AktifProjeler - gecenAyAktifProjeler) / gecenAyAktifProjeler) * 100, 1) : 0;
                TamamlananProjelerArtis = gecenAyTamamlananProjeler > 0 ? 
                    Math.Round(((double)(TamamlananProjeler - gecenAyTamamlananProjeler) / gecenAyTamamlananProjeler) * 100, 1) : 0;
                BekleyenProjelerArtis = gecenAyBekleyenProjeler > 0 ? 
                    Math.Round(((double)(BekleyenProjeler - gecenAyBekleyenProjeler) / gecenAyBekleyenProjeler) * 100, 1) : 0;
                
                // Haftalık istatistikler
                HaftalikYeniProjeler = await _context.Projeler
                    .CountAsync(p => p.bas.HasValue && p.bas >= buHafta);
                HaftalikTamamlananProjeler = await _context.Projeler
                    .CountAsync(p => p.Durum == 4 && p.bit.HasValue && p.bit >= buHafta);
                HaftalikIlerlemeler = await _context.Ilerlemeler
                    .CountAsync(i => i.IlerlemeTarihi >= buHafta);
                AktifKullanicilar = await _context.SystemLogs
                    .Where(log => log.CreatedAt >= buHafta && (log.LogType == "Kullanıcı Girişi" || log.LogType.Contains("Eklendi") || log.LogType.Contains("Güncellendi")))
                    .Select(log => log.Executor)
                    .Distinct()
                    .CountAsync();
                
                // Son aktiviteleri getir (login/logout hariç, sadece 10 tane)
                SonAktiviteler = await _context.SystemLogs
                    .Where(log => log.LogType != "Kullanıcı Girişi" && log.LogType != "Kullanıcı Çıkışı")
                    .OrderByDescending(log => log.CreatedAt)
                    .Take(10)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard istatistikleri yüklenirken hata oluştu");
                // Hata durumunda varsayılan değerler
                ToplamProjeler = 0;
                AktifProjeler = 0;
                TamamlananProjeler = 0;
                BekleyenProjeler = 0;
                ToplamProjelerArtis = 0;
                AktifProjelerArtis = 0;
                TamamlananProjelerArtis = 0;
                BekleyenProjelerArtis = 0;
                HaftalikYeniProjeler = 0;
                HaftalikTamamlananProjeler = 0;
                HaftalikIlerlemeler = 0;
                AktifKullanicilar = 0;
                SonAktiviteler = new List<SystemLog>();
            }

            return Page();
        }
    }
}
