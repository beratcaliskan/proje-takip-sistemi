using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Models;
using System.ComponentModel.DataAnnotations;

namespace ProjeTakip.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ProjeTakipContext _context;

        public LoginModel(ProjeTakipContext context)
        {
            _context = context;
        }

        [BindProperty]
        [Required(ErrorMessage = "Kimlik numarası gereklidir.")]
        [Display(Name = "Personel Kimlik No")]
        public string Kimlik { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public void OnGet()
        {
            // Session'ı temizle (logout işlemi)
            HttpContext.Session.Clear();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Veritabanında kullanıcıyı ara
                var kullanici = await _context.Kullanicilar
                    .FirstOrDefaultAsync(k => k.Kimlik == Kimlik);

                if (kullanici == null)
                {
                    ErrorMessage = "Girilen kimlik numarası sistemde bulunamadı. Lütfen doğru kimlik numarasını giriniz.";
                    return Page();
                }

                // Başarılı giriş - Session'a kullanıcı bilgilerini kaydet
                HttpContext.Session.SetInt32("UserId", kullanici.id);
                HttpContext.Session.SetString("UserName", kullanici.AdSoyad);
                HttpContext.Session.SetString("UserKimlik", kullanici.Kimlik);
                HttpContext.Session.SetInt32("UserRole", kullanici.Rol);

                // Ana sayfaya yönlendir
                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Giriş işlemi sırasında bir hata oluştu. Lütfen tekrar deneyiniz.";
                // Log the exception (in a real application)
                return Page();
            }
        }
    }
}