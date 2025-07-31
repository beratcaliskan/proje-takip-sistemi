using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Models;
using System.ComponentModel.DataAnnotations;

namespace ProjeTakip.Pages.Users
{
    public class IndexModel : PageModel
    {
        private readonly ProjeTakipContext _context;

        public IndexModel(ProjeTakipContext context)
        {
            _context = context;
        }

        [BindProperty]
        [Required(ErrorMessage = "Kimlik numarası gereklidir.")]
        [Display(Name = "Kimlik No")]
        public string Kimlik { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Ad Soyad gereklidir.")]
        [Display(Name = "Ad Soyad")]
        public string AdSoyad { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Rol seçimi gereklidir.")]
        [Display(Name = "Rol")]
        public int Rol { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;
        public List<Kullanici> Kullanicilar { get; set; } = new List<Kullanici>();

        [BindProperty]
        public int EditUserId { get; set; }
        
        [BindProperty]
        public string EditKimlik { get; set; } = string.Empty;
        
        [BindProperty]
        public string EditAdSoyad { get; set; } = string.Empty;
        
        [BindProperty]
        public int EditRol { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                Kullanicilar = await _context.Kullanicilar.ToListAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Kullanıcılar yüklenirken bir hata oluştu.";
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Kullanıcı listesini yeniden yükle
            await OnGetAsync();

            // Debug bilgisi
            Console.WriteLine($"POST Request - Kimlik: {Kimlik}, AdSoyad: {AdSoyad}, Rol: {Rol}");
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"ModelState Error - Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
                return Page();
            }

            try
            {
                // Aynı kimlik numarasında kullanıcı var mı kontrol et
                var existingUser = await _context.Kullanicilar
                    .FirstOrDefaultAsync(k => k.Kimlik == Kimlik);

                if (existingUser != null)
                {
                    ErrorMessage = "Bu kimlik numarası ile kayıtlı bir kullanıcı zaten mevcut.";
                    return Page();
                }

                // Yeni kullanıcı oluştur
                var yeniKullanici = new Kullanici
                {
                    Kimlik = Kimlik,
                    AdSoyad = AdSoyad,
                    Rol = Rol
                };

                Console.WriteLine($"Yeni kullanıcı oluşturuluyor: {yeniKullanici.Kimlik}, {yeniKullanici.AdSoyad}, {yeniKullanici.Rol}");

                _context.Kullanicilar.Add(yeniKullanici);
                var result = await _context.SaveChangesAsync();
                
                Console.WriteLine($"SaveChanges sonucu: {result} satır etkilendi");

                SuccessMessage = "Kullanıcı başarıyla eklendi.";
                
                // Form alanlarını temizle
                Kimlik = string.Empty;
                AdSoyad = string.Empty;
                Rol = 0;

                // Kullanıcı listesini yeniden yükle
                await OnGetAsync();

                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                ErrorMessage = $"Kullanıcı eklenirken bir hata oluştu: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var kullanici = await _context.Kullanicilar.FindAsync(id);
                if (kullanici != null)
                {
                    _context.Kullanicilar.Remove(kullanici);
                    await _context.SaveChangesAsync();
                    SuccessMessage = "Kullanıcı başarıyla silindi.";
                }
                else
                {
                    ErrorMessage = "Silinecek kullanıcı bulunamadı.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Kullanıcı silinirken bir hata oluştu.";
            }

            await OnGetAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            try
            {
                // Debug bilgisi
                Console.WriteLine($"Edit POST Request - EditUserId: {EditUserId}, EditKimlik: {EditKimlik}, EditAdSoyad: {EditAdSoyad}, EditRol: {EditRol}");
                
                if (EditUserId == 0)
                {
                    Console.WriteLine("EditUserId is 0!");
                    ErrorMessage = "Kullanıcı ID'si bulunamadı.";
                    await OnGetAsync();
                    return Page();
                }

                var kullanici = await _context.Kullanicilar.FindAsync(EditUserId);
                Console.WriteLine($"Bulunan kullanıcı: {(kullanici != null ? $"ID: {kullanici.id}, Kimlik: {kullanici.Kimlik}" : "null")}");
                
                if (kullanici != null)
                {
                    // Kimlik değişikliği kontrolü
                    if (kullanici.Kimlik != EditKimlik)
                    {
                        var existingUser = await _context.Kullanicilar
                            .FirstOrDefaultAsync(k => k.Kimlik == EditKimlik && k.id != EditUserId);
                        if (existingUser != null)
                        {
                            ErrorMessage = "Bu kimlik numarası başka bir kullanıcı tarafından kullanılıyor.";
                            await OnGetAsync();
                            return Page();
                        }
                    }

                    Console.WriteLine($"Güncelleme öncesi: Kimlik: {kullanici.Kimlik}, AdSoyad: {kullanici.AdSoyad}, Rol: {kullanici.Rol}");
                    
                    kullanici.Kimlik = EditKimlik;
                    kullanici.AdSoyad = EditAdSoyad;
                    kullanici.Rol = EditRol;

                    Console.WriteLine($"Güncelleme sonrası: Kimlik: {kullanici.Kimlik}, AdSoyad: {kullanici.AdSoyad}, Rol: {kullanici.Rol}");

                    var result = await _context.SaveChangesAsync();
                    Console.WriteLine($"Edit SaveChanges sonucu: {result} satır etkilendi");
                    
                    SuccessMessage = "Kullanıcı başarıyla güncellendi.";
                }
                else
                {
                    ErrorMessage = "Güncellenecek kullanıcı bulunamadı.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Edit Hata: {ex.Message}");
                Console.WriteLine($"Edit Stack Trace: {ex.StackTrace}");
                ErrorMessage = $"Kullanıcı güncellenirken bir hata oluştu: {ex.Message}";
            }

            await OnGetAsync();
            return Page();
        }
    }
}