using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Models;
using ProjeTakip.Services;
using System.ComponentModel.DataAnnotations;

namespace ProjeTakip.Pages.Units
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

        public IList<Birim> Birimler { get; set; } = default!;

        [BindProperty]
        public Birim YeniBirim { get; set; } = new Birim();

        [BindProperty]
        public Birim DuzenlenecekBirim { get; set; } = new Birim();

        public async Task<IActionResult> OnGetAsync()
        {
            // Sadece rol 1 (Admin) erişebilir
            var userRole = HttpContext.Session.GetInt32("UserRole");
            if (userRole != 1)
            {
                return RedirectToPage("/Index");
            }
            
            if (_context.Birimler != null)
            {
                Birimler = await _context.Birimler.ToListAsync();
            }
            
            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            _context.Birimler.Add(YeniBirim);
            await _context.SaveChangesAsync();
            
            // Birim ekleme işlemini logla
            var executor = HttpContext.Session.GetString("UserName") ?? "System";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            await _logService.LogUnitAddedAsync(YeniBirim.BirimAd, executor, ipAddress);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            _context.Attach(DuzenlenecekBirim).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                
                // Birim güncelleme işlemini logla
                var executor = HttpContext.Session.GetString("UserName") ?? "System";
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                await _logService.LogUnitUpdatedAsync(DuzenlenecekBirim.BirimAd, executor, ipAddress);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BirimExists(DuzenlenecekBirim.id))
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

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (_context.Birimler == null)
            {
                return NotFound();
            }

            var birim = await _context.Birimler.FindAsync(id);
            if (birim != null)
            {
                // Silme işlemini logla (silmeden önce bilgileri al)
                var executor = HttpContext.Session.GetString("UserName") ?? "System";
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var deletedUnitName = birim.BirimAd;
                
                _context.Birimler.Remove(birim);
                await _context.SaveChangesAsync();
                
                // Birim silme işlemini logla
                await _logService.LogUnitDeletedAsync(deletedUnitName, executor, ipAddress);
            }

            return RedirectToPage();
        }

        public async Task<JsonResult> OnGetBirimAsync(int id)
        {
            var birim = await _context.Birimler.FindAsync(id);
            if (birim == null)
            {
                return new JsonResult(new { success = false });
            }

            return new JsonResult(new
            {
                success = true,
                id = birim.id,
                birimAd = birim.BirimAd
            });
        }

        private bool BirimExists(int id)
        {
            return (_context.Birimler?.Any(e => e.id == id)).GetValueOrDefault();
        }
    }
}