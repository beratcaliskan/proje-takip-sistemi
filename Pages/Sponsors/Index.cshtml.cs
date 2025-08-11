using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Models;

namespace ProjeTakip.Pages.Sponsors
{
    public class IndexModel : PageModel
    {
        private readonly ProjeTakipContext _context;

        public IndexModel(ProjeTakipContext context)
        {
            _context = context;
        }

        public IList<Sponsor> Sponsorlar { get; set; } = default!;
        public IList<Birim> Birimler { get; set; } = default!;

        [BindProperty]
        public Sponsor AddSponsor { get; set; } = default!;

        [BindProperty]
        public Sponsor EditSponsor { get; set; } = default!;

        public async Task OnGetAsync()
        {
            if (_context.Sponsorler != null)
            {
                Sponsorlar = await _context.Sponsorler.ToListAsync();
            }
            
            if (_context.Birimler != null)
            {
                Birimler = await _context.Birimler.ToListAsync();
            }
        }

        public async Task<IActionResult> OnPostAddSponsorAsync()
        {
            if (!ModelState.IsValid || _context.Sponsorler == null || AddSponsor == null)
            {
                return Page();
            }

            _context.Sponsorler.Add(AddSponsor);
            await _context.SaveChangesAsync();

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
                _context.Sponsorler.Remove(sponsor);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        private bool SponsorExists(int id)
        {
            return (_context.Sponsorler?.Any(e => e.id == id)).GetValueOrDefault();
        }
    }
}