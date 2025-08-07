using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjeTakip.Pages.Timeline
{
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
            // Timeline sayfası için herhangi bir özel işlem gerekmiyor
            // Statik timeline içeriği doğrudan view'da tanımlanmış
        }
    }
}