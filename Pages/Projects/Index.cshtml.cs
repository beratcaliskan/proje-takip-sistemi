using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Models;
using System.ComponentModel.DataAnnotations;

namespace ProjeTakip.Pages.Projects
{
    public class IndexModel : PageModel
    {
        private readonly ProjeTakipContext _context;

        public IndexModel(ProjeTakipContext context)
        {
            _context = context;
        }

        [TempData]
        public string ErrorMessage { get; set; } = string.Empty;
        [TempData]
        public string SuccessMessage { get; set; } = string.Empty;
        
        public List<Proje> Projeler { get; set; } = new List<Proje>();
        public List<Proje> ProjeListe { get; set; } = new List<Proje>();
        public List<Birim> Birimler { get; set; } = new List<Birim>();
        public List<Kullanici> Kullanicilar { get; set; } = new List<Kullanici>();

        // İstatistik verileri
        public List<MudurlukIstatistik> MudurlukIstatistikleri { get; set; } = new();
        public List<DepartmanIstatistik> DepartmanIstatistikleri { get; set; } = new();
        public List<AylikIstatistik> AylikIstatistikler { get; set; } = new();
        public List<AsamaIstatistik> AsamaIstatistikleri { get; set; } = new();

        // Proje ekleme için model
        [BindProperty]
        public AddProjectModel AddProject { get; set; } = new AddProjectModel();

        // Proje düzenleme için model
        [BindProperty]
        public EditProjectModel EditProject { get; set; } = new EditProjectModel();

        public class AddProjectModel
        {
            [Required(ErrorMessage = "Proje adı gereklidir.")]
            [StringLength(200, MinimumLength = 2, ErrorMessage = "Proje adı 2-200 karakter arasında olmalıdır.")]
            public string ProjeAd { get; set; } = string.Empty;

            [Required(ErrorMessage = "Müdürlük gereklidir.")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "Müdürlük 2-100 karakter arasında olmalıdır.")]
            public string Mudurluk { get; set; } = string.Empty;

            [Required(ErrorMessage = "Başkanlık gereklidir.")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "Başkanlık 2-100 karakter arasında olmalıdır.")]
            public string Baskanlik { get; set; } = string.Empty;

            public int? BirimId { get; set; }
            public string? Amac { get; set; }
            public string? Kapsam { get; set; }
            public decimal? Maliyet { get; set; }
            public string? Ekip { get; set; }
            public DateTime? bas { get; set; }
            public DateTime? bit { get; set; }
            public string? olcut { get; set; }
            public string? sponsor { get; set; }

            [Required(ErrorMessage = "Durum seçimi gereklidir.")]
            [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir durum seçiniz.")]
            public int Durum { get; set; }

            [Required(ErrorMessage = "Personel sayısı gereklidir.")]
            [Range(1, int.MaxValue, ErrorMessage = "Personel sayısı 1 veya daha fazla olmalıdır.")]
            public int personel { get; set; }
        }

        public class EditProjectModel
        {
            public int ProjeID { get; set; }

            [Required(ErrorMessage = "Proje adı gereklidir.")]
            [StringLength(200, MinimumLength = 2, ErrorMessage = "Proje adı 2-200 karakter arasında olmalıdır.")]
            public string ProjeAd { get; set; } = string.Empty;

            [Required(ErrorMessage = "Müdürlük gereklidir.")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "Müdürlük 2-100 karakter arasında olmalıdır.")]
            public string Mudurluk { get; set; } = string.Empty;

            [Required(ErrorMessage = "Başkanlık gereklidir.")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "Başkanlık 2-100 karakter arasında olmalıdır.")]
            public string Baskanlik { get; set; } = string.Empty;

            public int? BirimId { get; set; }
            public string? Amac { get; set; }
            public string? Kapsam { get; set; }
            public decimal? Maliyet { get; set; }
            public string? Ekip { get; set; }
            public DateTime? bas { get; set; }
            public DateTime? bit { get; set; }
            public string? olcut { get; set; }
            public string? sponsor { get; set; }

            [Required(ErrorMessage = "Durum seçimi gereklidir.")]
            [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir durum seçiniz.")]
            public int Durum { get; set; }

            [Required(ErrorMessage = "Personel sayısı gereklidir.")]
            [Range(1, int.MaxValue, ErrorMessage = "Personel sayısı 1 veya daha fazla olmalıdır.")]
            public int personel { get; set; }
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
                Projeler = await _context.Projeler.ToListAsync();
                ProjeListe = Projeler; // ProjeListe'yi de doldur
                Birimler = await _context.Birimler.ToListAsync();
                Kullanicilar = await _context.Kullanicilar.ToListAsync();
                
                // İstatistik verilerini yükle
                await LoadStatisticsAsync();
            }
            catch
            {
                ErrorMessage = "Projeler yüklenirken bir hata oluştu.";
                Projeler = new List<Proje>();
            }
            
            return Page();
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                // Müdürlük istatistikleri
                MudurlukIstatistikleri = await _context.Projeler
                    .GroupBy(p => p.Mudurluk)
                    .Select(g => new MudurlukIstatistik
                    {
                        MudurlukAdi = g.Key,
                        ProjeSayisi = g.Count(),
                        ToplamMaliyet = g.Sum(p => p.Maliyet ?? 0),
                        OrtalamaMaliyet = g.Average(p => p.Maliyet ?? 0)
                    })
                    .ToListAsync();

                // Departman istatistikleri
                DepartmanIstatistikleri = await _context.Projeler
                    .GroupBy(p => p.Baskanlik)
                    .Select(g => new DepartmanIstatistik
                    {
                        DepartmanAdi = g.Key,
                        ProjeSayisi = g.Count(),
                        ToplamMaliyet = g.Sum(p => p.Maliyet ?? 0),
                        OrtalamaMaliyet = g.Average(p => p.Maliyet ?? 0)
                    })
                    .ToListAsync();

                // Aylık istatistikler
                AylikIstatistikler = await _context.Projeler
                    .Where(p => p.bas.HasValue)
                    .GroupBy(p => p.bas.Value.ToString("yyyy-MM"))
                    .Select(g => new AylikIstatistik
                    {
                        Ay = g.Key,
                        ProjeSayisi = g.Count(),
                        ToplamMaliyet = g.Sum(p => p.Maliyet ?? 0),
                        OrtalamaMaliyet = g.Average(p => p.Maliyet ?? 0)
                    })
                    .ToListAsync();

                // Aşama istatistikleri
                var toplamProje = await _context.Projeler.CountAsync();
                AsamaIstatistikleri = await _context.Projeler
                    .GroupBy(p => p.Durum)
                    .Select(g => new AsamaIstatistik
                    {
                        AsamaAdi = g.Key == 1 ? "Başlangıç" : g.Key == 2 ? "Devam Ediyor" : g.Key == 3 ? "Tamamlandı" : "Diğer",
                        ProjeSayisi = g.Count(),
                        Yuzde = toplamProje > 0 ? (double)g.Count() / toplamProje * 100 : 0
                    })
                    .ToListAsync();
            }
            catch
            {
                // Hata durumunda boş listeler
                MudurlukIstatistikleri = new List<MudurlukIstatistik>();
                DepartmanIstatistikleri = new List<DepartmanIstatistik>();
                AylikIstatistikler = new List<AylikIstatistik>();
                AsamaIstatistikleri = new List<AsamaIstatistik>();
            }
        }

        // Proje ekleme
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
            
            // Önce proje listesini yükle
            await OnGetAsync();

            // Sadece AddProject modeli için validation
            ModelState.Clear();
            TryValidateModel(AddProject, nameof(AddProject));

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
                // Yeni proje oluştur
                var yeniProje = new Proje
                {
                    ProjeAd = AddProject.ProjeAd,
                    Mudurluk = AddProject.Mudurluk,
                    Baskanlik = AddProject.Baskanlik,
                    BirimId = AddProject.BirimId,
                    Amac = AddProject.Amac,
                    Kapsam = AddProject.Kapsam,
                    Maliyet = AddProject.Maliyet,
                    Ekip = AddProject.Ekip,
                    bas = AddProject.bas,
                    bit = AddProject.bit,
                    olcut = AddProject.olcut,
                    sponsor = AddProject.sponsor,
                    Durum = AddProject.Durum,
                    personel = AddProject.personel
                };

                _context.Projeler.Add(yeniProje);
                await _context.SaveChangesAsync();

                var successMsg = "Proje başarıyla eklendi.";
                
                if (isAjax)
                {
                    return new JsonResult(new { success = true, message = successMsg });
                }
                
                SuccessMessage = successMsg;
                
                // Form temizle
                AddProject = new AddProjectModel();
                
                // Listeyi yenile
                await OnGetAsync();
                
                return Page();
            }
            catch
            {
                var errorMsg = "Proje eklenirken bir hata oluştu.";
                if (isAjax)
                {
                    return new JsonResult(new { success = false, message = errorMsg });
                }
                ErrorMessage = errorMsg;
                return Page();
            }
        }

        // Proje düzenleme
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
            
            // Önce proje listesini yükle
            await OnGetAsync();

            // Sadece EditProject modeli için validation
            ModelState.Clear();
            TryValidateModel(EditProject, nameof(EditProject));

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
                if (EditProject.ProjeID <= 0)
                {
                    var errorMsg = "Geçersiz proje ID'si.";
                    if (isAjax)
                    {
                        return new JsonResult(new { success = false, message = errorMsg });
                    }
                    ErrorMessage = errorMsg;
                    return Page();
                }

                var proje = await _context.Projeler.FindAsync(EditProject.ProjeID);
                
                if (proje == null)
                {
                    var errorMsg = "Güncellenecek proje bulunamadı.";
                    if (isAjax)
                    {
                        return new JsonResult(new { success = false, message = errorMsg });
                    }
                    ErrorMessage = errorMsg;
                    return Page();
                }

                // Güncelle
                proje.ProjeAd = EditProject.ProjeAd;
                proje.Mudurluk = EditProject.Mudurluk;
                proje.Baskanlik = EditProject.Baskanlik;
                proje.BirimId = EditProject.BirimId;
                proje.Amac = EditProject.Amac;
                proje.Kapsam = EditProject.Kapsam;
                proje.Maliyet = EditProject.Maliyet;
                proje.Ekip = EditProject.Ekip;
                proje.bas = EditProject.bas;
                proje.bit = EditProject.bit;
                proje.olcut = EditProject.olcut;
                proje.sponsor = EditProject.sponsor;
                proje.Durum = EditProject.Durum;
                proje.personel = EditProject.personel;

                await _context.SaveChangesAsync();
                
                var successMsg = "Proje başarıyla güncellendi.";
                
                if (isAjax)
                {
                    return new JsonResult(new { success = true, message = successMsg });
                }
                
                SuccessMessage = successMsg;
                
                // Form temizle
                EditProject = new EditProjectModel();
                
                // Listeyi yenile
                await OnGetAsync();
                
                return Page();
            }
            catch
            {
                var errorMsg = "Proje güncellenirken bir hata oluştu.";
                if (isAjax)
                {
                    return new JsonResult(new { success = false, message = errorMsg });
                }
                ErrorMessage = errorMsg;
                return Page();
            }
        }

        // Proje silme
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
            
            // Önce proje listesini yükle
            await OnGetAsync();

            if (id <= 0)
            {
                var errorMsg = "Geçersiz proje ID'si.";
                if (isAjax)
                {
                    return new JsonResult(new { success = false, message = errorMsg });
                }
                ErrorMessage = errorMsg;
                return Page();
            }

            try
            {
                var proje = await _context.Projeler.FindAsync(id);
                
                if (proje == null)
                {
                    var errorMsg = "Silinecek proje bulunamadı.";
                    if (isAjax)
                    {
                        return new JsonResult(new { success = false, message = errorMsg });
                    }
                    ErrorMessage = errorMsg;
                    return Page();
                }

                _context.Projeler.Remove(proje);
                await _context.SaveChangesAsync();

                var successMsg = "Proje başarıyla silindi.";
                
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
                var errorMsg = "Proje silinirken bir hata oluştu.";
                if (isAjax)
                {
                    return new JsonResult(new { success = false, message = errorMsg });
                }
                ErrorMessage = errorMsg;
                return Page();
            }
        }

        // Proje verilerini getir
        public async Task<IActionResult> OnGetProjectDataAsync(int projectId)
        {
            try
            {
                var proje = await _context.Projeler.FindAsync(projectId);
                
                if (proje == null)
                {
                    return new JsonResult(new { success = false, message = "Proje bulunamadı." });
                }

                var projectData = new {
                    projeID = proje.ProjeID,
                    projeAd = proje.ProjeAd,
                    mudurluk = proje.Mudurluk,
                    baskanlik = proje.Baskanlik,
                    birimId = proje.BirimId,
                    amac = proje.Amac,
                    kapsam = proje.Kapsam,
                    maliyet = proje.Maliyet,
                    ekip = proje.Ekip,
                    bas = proje.bas?.ToString("yyyy-MM-dd"),
                    bit = proje.bit?.ToString("yyyy-MM-dd"),
                    olcut = proje.olcut,
                    sponsor = proje.sponsor,
                    durum = proje.Durum,
                    personel = proje.personel
                };

                return new JsonResult(new { success = true, data = projectData });
            }
            catch
            {
                return new JsonResult(new { success = false, message = "Proje verileri alınırken bir hata oluştu." });
            }
        }

        public async Task<IActionResult> OnGetProjectProgressAsync(int projectId)
        {
            try
            {
                var progressList = await _context.Ilerlemeler
                    .Include(i => i.GanttAsama)
                    .Include(i => i.EkleyenKullanici)
                    .Where(i => i.ProjeID == projectId)
                    .OrderByDescending(i => i.IlerlemeTarihi)
                    .Select(i => new {
                        id = i.id,
                        ganttAsama = i.GanttAsama != null ? i.GanttAsama.Asama : "Tanımsız",
                        ilerleme = i.IlerlemeTanimi,
                        yuzde = i.TamamlanmaYuzdesi,
                        tarih = i.IlerlemeTarihi.ToString("dd.MM.yyyy"),
                        ekleyenKullanici = i.EkleyenKullanici != null ? i.EkleyenKullanici.AdSoyad : "Bilinmiyor"
                    })
                    .ToListAsync();

                return new JsonResult(new { success = true, progressList = progressList });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        // İlerleme verilerini getir
        public async Task<IActionResult> OnGetGetProgressAsync(int id)
        {
            try
            {
                var ilerleme = await _context.Ilerlemeler
                    .Include(i => i.GanttAsama)
                    .FirstOrDefaultAsync(i => i.id == id);
                
                if (ilerleme == null)
                {
                    return new JsonResult(new { success = false, message = "İlerleme bulunamadı." });
                }

                var progressData = new {
                    id = ilerleme.id,
                    projeId = ilerleme.ProjeID,
                    ganttId = ilerleme.GanttID,
                    ilerleme = ilerleme.IlerlemeTanimi,
                    yuzde = ilerleme.TamamlanmaYuzdesi,
                    aciklama = ilerleme.Aciklama
                };

                return new JsonResult(new { success = true, data = progressData });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "İlerleme verileri alınırken bir hata oluştu: " + ex.Message });
            }
        }

        // Gantt aşamalarını getir
        public async Task<IActionResult> OnGetGanttStagesAsync(int projeId)
        {
            try
            {
                var ganttStages = await _context.GanttAsamalari
                    .Where(g => g.ProjeID == projeId)
                    .OrderBy(g => g.Sira)
                    .Select(g => new {
                        id = g.id,
                        asama = g.Asama
                    })
                    .ToListAsync();

                return new JsonResult(new { success = true, stages = ganttStages });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Gantt aşamaları alınırken bir hata oluştu: " + ex.Message });
            }
        }
    }

    // İstatistik sınıfları
    public class MudurlukIstatistik
    {
        public string MudurlukAdi { get; set; }
        public int ProjeSayisi { get; set; }
        public decimal ToplamMaliyet { get; set; }
        public decimal OrtalamaMaliyet { get; set; }
    }

    public class DepartmanIstatistik
    {
        public string DepartmanAdi { get; set; }
        public int ProjeSayisi { get; set; }
        public decimal ToplamMaliyet { get; set; }
        public decimal OrtalamaMaliyet { get; set; }
    }

    public class AylikIstatistik
    {
        public string Ay { get; set; }
        public int ProjeSayisi { get; set; }
        public decimal ToplamMaliyet { get; set; }
        public decimal OrtalamaMaliyet { get; set; }
    }

    public class AsamaIstatistik
    {
        public string AsamaAdi { get; set; }
        public int ProjeSayisi { get; set; }
        public double Yuzde { get; set; }
    }
}