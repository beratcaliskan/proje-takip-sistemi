using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Models;
using ProjeTakip.Services;

namespace ProjeTakip.Pages.Sponsors
{
    public class IndexModel : PageModel
    {
        private readonly ProjeTakipContext _context;
        private readonly ISystemLogService _logService;

        public IndexModel(ProjeTakipContext context, ISystemLogService logService)
        {
            _context = context;
            _logService = logService;
        }

        public IList<Sponsor> Sponsorlar { get; set; } = default!;
        public IList<Birim> Birimler { get; set; } = default!;

        [BindProperty]
        public Sponsor AddSponsor { get; set; } = default!;

        [BindProperty]
        public Sponsor EditSponsor { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            // Sadece rol 1 (Admin) erişebilir
            var userRole = HttpContext.Session.GetInt32("UserRole");
            if (userRole != 1)
            {
                return RedirectToPage("/Index");
            }
            
            if (_context.Sponsorler != null)
            {
                Sponsorlar = await _context.Sponsorler.ToListAsync();
            }
            
            if (_context.Birimler != null)
            {
                Birimler = await _context.Birimler.ToListAsync();
            }
            
            return Page();
        }

        public async Task<IActionResult> OnPostAddSponsorAsync()
        {
            if (!ModelState.IsValid || _context.Sponsorler == null || AddSponsor == null)
            {
                return Page();
            }

            _context.Sponsorler.Add(AddSponsor);
            await _context.SaveChangesAsync();
            
            // Sponsor ekleme işlemini logla
            var executor = HttpContext.Session.GetString("UserName") ?? "System";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            await _logService.LogSponsorAddedAsync(AddSponsor.SponsorAd, executor, ipAddress);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditSponsorAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(EditSponsor).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                
                // Sponsor güncelleme işlemini logla
                var executor = HttpContext.Session.GetString("UserName") ?? "System";
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                await _logService.LogSponsorUpdatedAsync(EditSponsor.SponsorAd, executor, ipAddress);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SponsorExists(EditSponsor.id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteSponsorAsync(int id)
        {
            if (_context.Sponsorler == null)
            {
                return Page();
            }

            var sponsor = await _context.Sponsorler.FindAsync(id);

            if (sponsor != null)
            {
                // Silme işlemini logla (silmeden önce bilgileri al)
                var executor = HttpContext.Session.GetString("UserName") ?? "System";
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var deletedSponsorName = sponsor.SponsorAd;
                
                _context.Sponsorler.Remove(sponsor);
                await _context.SaveChangesAsync();
                
                // Sponsor silme işlemini logla
                await _logService.LogSponsorDeletedAsync(deletedSponsorName, executor, ipAddress);
            }

            return RedirectToPage();
        }

        private bool SponsorExists(int id)
        {
            return (_context.Sponsorler?.Any(e => e.id == id)).GetValueOrDefault();
        }
    }
}