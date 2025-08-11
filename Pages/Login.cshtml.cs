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
        [Required(ErrorMessage = "Kullanıcı ID gereklidir.")]
        [Display(Name = "Kullanıcı ID")]
        public string Kimlik { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Şifre gereklidir.")]
        [Display(Name = "Şifre")]
        public string Sifre { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public void OnGet()
        {
            // Session'ı temizle (logout işlemi)
            HttpContext.Session.Clear();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // AJAX isteği kontrolü
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            
            if (!ModelState.IsValid)
            {
                if (isAjax)
                {
                    return new JsonResult(new { success = false, message = "Lütfen kullanıcı ID ve şifrenizi giriniz." });
                }
                return Page();
            }

            try
            {
                // Veritabanında kullanıcıyı ara
                var kullanici = await _context.Kullanicilar
                    .FirstOrDefaultAsync(k => k.Kimlik == Kimlik && k.Sifre == Sifre);

                if (kullanici == null)
                {
                    var errorMsg = "Kullanıcı ID veya şifre hatalı. Lütfen bilgilerinizi kontrol ediniz.";
                    if (isAjax)
                    {
                        return new JsonResult(new { success = false, message = errorMsg });
                    }
                    ErrorMessage = errorMsg;
                    return Page();
                }

                // Başarılı giriş - Session'a kullanıcı bilgilerini kaydet
                HttpContext.Session.SetInt32("UserId", kullanici.id);
                HttpContext.Session.SetString("UserName", kullanici.AdSoyad);
                HttpContext.Session.SetString("UserKimlik", kullanici.Kimlik);
                HttpContext.Session.SetInt32("UserRole", kullanici.Rol);

                if (isAjax)
                {
                    return new JsonResult(new { success = true, message = "Giriş başarılı. Yönlendiriliyorsunuz...", redirectUrl = "/Index" });
                }

                // Ana sayfaya yönlendir
                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                var errorMsg = "Giriş işlemi sırasında bir hata oluştu. Lütfen tekrar deneyiniz.";
                if (isAjax)
                {
                    return new JsonResult(new { success = false, message = errorMsg });
                }
                ErrorMessage = errorMsg;
                // Log the exception (in a real application)
                return Page();
            }
        }
    }
}