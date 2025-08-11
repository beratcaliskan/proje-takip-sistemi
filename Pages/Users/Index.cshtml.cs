using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Models;
using ProjeTakip.Services;
using System.ComponentModel.DataAnnotations;

namespace ProjeTakip.Pages.Users
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

        [TempData]
        public string ErrorMessage { get; set; } = string.Empty;
        [TempData]
        public string SuccessMessage { get; set; } = string.Empty;
        
        public List<Kullanici> Kullanicilar { get; set; } = new List<Kullanici>();

        // Kullanıcı ekleme için model
        [BindProperty]
        public AddUserModel AddUser { get; set; } = new AddUserModel();

        // Kullanıcı düzenleme için model
        [BindProperty]
        public EditUserModel EditUser { get; set; } = new EditUserModel();

        public class AddUserModel
        {
            [Required(ErrorMessage = "Kullanıcı ID gereklidir.")]
            [StringLength(50, MinimumLength = 1, ErrorMessage = "Kullanıcı ID 1-50 karakter arasında olmalıdır.")]
            public string Kimlik { get; set; } = string.Empty;

            [Required(ErrorMessage = "Ad Soyad gereklidir.")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "Ad Soyad 2-100 karakter arasında olmalıdır.")]
            public string AdSoyad { get; set; } = string.Empty;

            [Required(ErrorMessage = "Rol seçimi gereklidir.")]
            [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir rol seçiniz.")]
            public int Rol { get; set; }
        }

        public class EditUserModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "Kullanıcı ID gereklidir.")]
            [StringLength(50, MinimumLength = 1, ErrorMessage = "Kullanıcı ID 1-50 karakter arasında olmalıdır.")]
            public string Kimlik { get; set; } = string.Empty;

            [Required(ErrorMessage = "Ad Soyad gereklidir.")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "Ad Soyad 2-100 karakter arasında olmalıdır.")]
            public string AdSoyad { get; set; } = string.Empty;

            [Required(ErrorMessage = "Rol seçimi gereklidir.")]
            [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir rol seçiniz.")]
            public int Rol { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Admin kontrolü
            var userRole = HttpContext.Session.GetInt32("UserRole");
            if (userRole != 1)
            {
                return RedirectToPage("/Index");
            }

            try
            {
                Kullanicilar = await _context.Kullanicilar.ToListAsync();
            }
            catch
            {
                ErrorMessage = "Kullanıcılar yüklenirken bir hata oluştu.";
                Kullanicilar = new List<Kullanici>();
            }
            
            return Page();
        }

        // Kullanıcı ekleme
        public async Task<IActionResult> OnPostAsync()
        {
            // Admin kontrolü
            var userRole = HttpContext.Session.GetInt32("UserRole");
            if (userRole != 1)
            {
                return RedirectToPage("/Index");
            }

            // AJAX isteği kontrolü
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            
            // Önce kullanıcı listesini yükle
            await OnGetAsync();

            // Sadece AddUser modeli için validation
            ModelState.Clear();
            TryValidateModel(AddUser, nameof(AddUser));

            if (!ModelState.IsValid)
            {
                // Validation hatalarını topla
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .SelectMany(x => x.Value.Errors)
                    .Select(x => x.ErrorMessage)
                    .ToList();
                
                var errorMessage = errors.Count > 0 ? string.Join(" ", errors) : "Lütfen tüm alanları doğru şekilde doldurun.";
                
                if (isAjax)
                {
                    return new JsonResult(new { success = false, message = errorMessage });
                }
                ErrorMessage = errorMessage;
                return Page();
            }

            try
            {
                // Aynı kimlik kontrolü
                var existingUser = await _context.Kullanicilar
                    .FirstOrDefaultAsync(k => k.Kimlik == AddUser.Kimlik);

                if (existingUser != null)
                {
                    var errorMsg = "Bu kullanıcı ID ile kayıtlı bir kullanıcı zaten mevcut.";
                    if (isAjax)
                    {
                        return new JsonResult(new { success = false, message = errorMsg });
                    }
                    ErrorMessage = errorMsg;
                    return Page();
                }

                // Yeni kullanıcı oluştur
                var yeniKullanici = new Kullanici
                {
                    Kimlik = AddUser.Kimlik,
                    AdSoyad = AddUser.AdSoyad,
                    Rol = AddUser.Rol
                };

                _context.Kullanicilar.Add(yeniKullanici);
                await _context.SaveChangesAsync();
                
                // Kullanıcı ekleme işlemini logla
                var executor = HttpContext.Session.GetString("UserName") ?? "System";
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                await _logService.LogUserAddedAsync(AddUser.Kimlik, executor, ipAddress);

                var successMsg = "Kullanıcı başarıyla eklendi.";
                
                if (isAjax)
                {
                    return new JsonResult(new { success = true, message = successMsg });
                }
                
                SuccessMessage = successMsg;
                
                // Form temizle
                AddUser = new AddUserModel();
                
                // Listeyi yenile
                await OnGetAsync();
                
                return Page();
            }
            catch
            {
                var errorMsg = "Kullanıcı eklenirken bir hata oluştu.";
                if (isAjax)
                {
                    return new JsonResult(new { success = false, message = errorMsg });
                }
                ErrorMessage = errorMsg;
                return Page();
            }
        }

        // Kullanıcı düzenleme
        public async Task<IActionResult> OnPostEditAsync()
        {
            // Admin kontrolü
            var userRole = HttpContext.Session.GetInt32("UserRole");
            if (userRole != 1)
            {
                return RedirectToPage("/Index");
            }

            // AJAX isteği kontrolü
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            
            // Önce kullanıcı listesini yükle
            await OnGetAsync();

            // Sadece EditUser modeli için validation
            ModelState.Clear();
            TryValidateModel(EditUser, nameof(EditUser));

            if (!ModelState.IsValid)
            {
                // Validation hatalarını topla
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .SelectMany(x => x.Value.Errors)
                    .Select(x => x.ErrorMessage)
                    .ToList();
                
                var errorMessage = errors.Count > 0 ? string.Join(" ", errors) : "Lütfen tüm alanları doğru şekilde doldurun.";
                
                if (isAjax)
                {
                    return new JsonResult(new { success = false, message = errorMessage });
                }
                ErrorMessage = errorMessage;
                return Page();
            }

            try
            {
                if (EditUser.Id <= 0)
                {
                    var errorMsg = "Geçersiz kullanıcı ID'si.";
                    if (isAjax)
                    {
                        return new JsonResult(new { success = false, message = errorMsg });
                    }
                    ErrorMessage = errorMsg;
                    return Page();
                }

                var kullanici = await _context.Kullanicilar.FindAsync(EditUser.Id);
                
                if (kullanici == null)
                {
                    var errorMsg = "Güncellenecek kullanıcı bulunamadı.";
                    if (isAjax)
                    {
                        return new JsonResult(new { success = false, message = errorMsg });
                    }
                    ErrorMessage = errorMsg;
                    return Page();
                }

                // Kimlik çakışma kontrolü
                var existingUser = await _context.Kullanicilar
                    .FirstOrDefaultAsync(k => k.Kimlik == EditUser.Kimlik && k.id != EditUser.Id);
                
                if (existingUser != null)
                {
                    var errorMsg = "Bu kullanıcı ID başka bir kullanıcı tarafından kullanılıyor.";
                    if (isAjax)
                    {
                        return new JsonResult(new { success = false, message = errorMsg });
                    }
                    ErrorMessage = errorMsg;
                    return Page();
                }

                // Güncelle
                kullanici.Kimlik = EditUser.Kimlik;
                kullanici.AdSoyad = EditUser.AdSoyad;
                kullanici.Rol = EditUser.Rol;

                await _context.SaveChangesAsync();
                
                // Kullanıcı güncelleme işlemini logla
                var executor = HttpContext.Session.GetString("UserName") ?? "System";
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                await _logService.LogUserUpdatedAsync(EditUser.Kimlik, executor, ipAddress);
                
                // Eğer güncellenecek kullanıcı şu anda giriş yapmış kullanıcıysa session'ı güncelle
                var currentUserId = HttpContext.Session.GetString("UserId");
                if (currentUserId != null && int.Parse(currentUserId) == EditUser.Id)
                {
                    HttpContext.Session.SetString("UserKimlik", kullanici.Kimlik ?? "");
                    HttpContext.Session.SetString("UserName", kullanici.AdSoyad ?? "");
                    HttpContext.Session.SetInt32("UserRole", kullanici.Rol);
                }
                
                var successMsg = "Kullanıcı başarıyla güncellendi.";
                
                if (isAjax)
                {
                    return new JsonResult(new { success = true, message = successMsg });
                }
                
                SuccessMessage = successMsg;
                
                // Form temizle
                EditUser = new EditUserModel();
                
                // Listeyi yenile
                await OnGetAsync();
                
                return Page();
            }
            catch
            {
                var errorMsg = "Kullanıcı güncellenirken bir hata oluştu.";
                if (isAjax)
                {
                    return new JsonResult(new { success = false, message = errorMsg });
                }
                ErrorMessage = errorMsg;
                return Page();
            }
        }

        // Kullanıcı silme
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            // Admin kontrolü
            var userRole = HttpContext.Session.GetInt32("UserRole");
            if (userRole != 1)
            {
                return RedirectToPage("/Index");
            }

            // AJAX isteği kontrolü
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            
            // Önce kullanıcı listesini yükle
            await OnGetAsync();

            if (id <= 0)
            {
                var errorMsg = "Geçersiz kullanıcı ID'si.";
                if (isAjax)
                {
                    return new JsonResult(new { success = false, message = errorMsg });
                }
                ErrorMessage = errorMsg;
                return Page();
            }

            try
            {
                var kullanici = await _context.Kullanicilar.FindAsync(id);
                
                if (kullanici == null)
                {
                    var errorMsg = "Silinecek kullanıcı bulunamadı.";
                    if (isAjax)
                    {
                        return new JsonResult(new { success = false, message = errorMsg });
                    }
                    ErrorMessage = errorMsg;
                    return Page();
                }

                // Silme işlemini logla (silmeden önce bilgileri al)
                var executor = HttpContext.Session.GetString("UserName") ?? "System";
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var deletedUserKimlik = kullanici.Kimlik;
                
                _context.Kullanicilar.Remove(kullanici);
                await _context.SaveChangesAsync();
                
                // Kullanıcı silme işlemini logla
                await _logService.LogUserDeletedAsync(deletedUserKimlik, executor, ipAddress);

                var successMsg = "Kullanıcı başarıyla silindi.";
                
                if (isAjax)
                {
                    return new JsonResult(new { success = true, message = successMsg });
                }
                
                SuccessMessage = successMsg;
                
                // Listeyi yenile
                await OnGetAsync();
                
                return Page();
            }
            catch
            {
                var errorMsg = "Kullanıcı silinirken bir hata oluştu.";
                if (isAjax)
                {
                    return new JsonResult(new { success = false, message = errorMsg });
                }
                ErrorMessage = errorMsg;
                return Page();
            }
        }
    }
}