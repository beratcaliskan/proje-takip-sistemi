using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Models;
using System.ComponentModel.DataAnnotations;

namespace ProjeTakip.Pages.Units
{
    public class IndexModel : PageModel
    {
        private readonly ProjeTakipContext _context;

        public IndexModel(ProjeTakipContext context)
        {
            _context = context;
        }

        public IList<Birim> Birimler { get; set; } = default!;

        [BindProperty]
        public Birim YeniBirim { get; set; } = new Birim();

        [BindProperty]
        public Birim DuzenlenecekBirim { get; set; } = new Birim();

        public async Task OnGetAsync()
        {
            if (_context.Birimler != null)
            {
                Birimler = await _context.Birimler.ToListAsync();
            }
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
                _context.Birimler.Remove(birim);
                await _context.SaveChangesAsync();
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