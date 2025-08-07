using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Models;

namespace ProjeTakip.Pages.Progress
{
    public class IndexModel : PageModel
    {
        private readonly ProjeTakipContext _context;

        public IndexModel(ProjeTakipContext context)
        {
            _context = context;
        }

        public IList<Ilerleme> IlerlemeListe { get; set; } = default!;
        public IList<Gantt> GanttListe { get; set; } = default!;
        public IList<Proje> ProjeListe { get; set; } = default!;

        [BindProperty]
        public Ilerleme AddIlerleme { get; set; } = default!;

        [BindProperty]
        public Gantt AddGantt { get; set; } = default!;

        public async Task OnGetAsync()
        {
            if (_context.Ilerlemeler != null)
            {
                IlerlemeListe = await _context.Ilerlemeler
                    .Include(i => i.Proje)
                    .Include(i => i.GanttAsama)
                    .Include(i => i.EkleyenKullanici)
                    .ToListAsync();
            }

            if (_context.GanttAsamalari != null)
            {
                GanttListe = await _context.GanttAsamalari
                    .Include(g => g.Proje)
                    .OrderBy(g => g.ProjeID)
                    .ThenBy(g => g.Sira)
                    .ToListAsync();
            }

            if (_context.Projeler != null)
            {
                ProjeListe = await _context.Projeler.ToListAsync();
            }
        }

        public async Task<IActionResult> OnPostAddIlerlemeAsync(int projeId, int ganttId, string ilerlemeTanimi, int tamamlanmaYuzdesi, string? aciklama)
        {
            if (projeId > 0 && ganttId > 0 && !string.IsNullOrEmpty(ilerlemeTanimi))
            {
                // Session'dan kullanıcı ID'sini al
                var kullaniciId = HttpContext.Session.GetInt32("UserId");

                var ilerleme = new Ilerleme
                {
                    ProjeID = projeId,
                    GanttID = ganttId,
                    IlerlemeTanimi = ilerlemeTanimi,
                    TamamlanmaYuzdesi = tamamlanmaYuzdesi,
                    IlerlemeTarihi = DateTime.Now,
                    Aciklama = aciklama,
                    KullaniciID = kullaniciId
                };
                _context.Ilerlemeler.Add(ilerleme);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddGanttAsync()
        {
            if (!ModelState.IsValid || _context.GanttAsamalari == null || AddGantt == null)
            {
                TempData["ErrorMessage"] = "Geçersiz veri girişi!";
                return RedirectToPage();
            }

            _context.GanttAsamalari.Add(AddGantt);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Gantt aşaması başarıyla eklendi!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateIlerlemeAsync(int id, int projeId, int ganttId, string ilerlemeTanimi, int tamamlanmaYuzdesi, string? aciklama)
        {
            try
            {
                var ilerleme = await _context.Ilerlemeler.FindAsync(id);
                if (ilerleme != null && projeId > 0 && ganttId > 0 && !string.IsNullOrEmpty(ilerlemeTanimi))
                {
                    ilerleme.ProjeID = projeId;
                    ilerleme.GanttID = ganttId;
                    ilerleme.IlerlemeTanimi = ilerlemeTanimi;
                    ilerleme.TamamlanmaYuzdesi = tamamlanmaYuzdesi;
                    ilerleme.Aciklama = aciklama;
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "İlerleme başarıyla güncellendi!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Geçersiz veri girişi veya ilerleme bulunamadı!";
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = "Bu ilerleme kaydı başka bir kullanıcı tarafından değiştirilmiş. Lütfen sayfayı yenileyin ve tekrar deneyin.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "İlerleme güncellenirken bir hata oluştu: " + ex.Message;
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateGanttAsync(int id, int projeId, string asama, DateTime? baslangic, DateTime? bitis, int gun, int sira)
        {
            try
            {
                var gantt = await _context.GanttAsamalari.FindAsync(id);
                if (gantt == null)
                {
                    TempData["ErrorMessage"] = "Gantt aşaması bulunamadı!";
                    return RedirectToPage();
                }

                gantt.ProjeID = projeId;
                gantt.Asama = asama;
                gantt.Baslangic = baslangic;
                gantt.Bitis = bitis;
                gantt.Gun = gun;
                gantt.Sira = sira;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Gantt aşaması başarıyla güncellendi!";
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = "Bu Gantt aşaması başka bir kullanıcı tarafından değiştirilmiş. Lütfen sayfayı yenileyin ve tekrar deneyin.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gantt aşaması güncellenirken bir hata oluştu: " + ex.Message;
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteIleremeAsync(int id)
        {
            try
            {
                var ilerleme = await _context.Ilerlemeler.FindAsync(id);
                if (ilerleme != null)
                {
                    _context.Ilerlemeler.Remove(ilerleme);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "İlerleme başarıyla silindi!";
                }
                else
                {
                    TempData["ErrorMessage"] = "İlerleme bulunamadı!";
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = "Bu ilerleme kaydı başka bir kullanıcı tarafından değiştirilmiş veya silinmiş. Lütfen sayfayı yenileyin ve tekrar deneyin.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "İlerleme silinirken bir hata oluştu: " + ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteGanttAsync(int id)
        {
            try
            {
                var gantt = await _context.GanttAsamalari.FindAsync(id);
                if (gantt != null)
                {
                    _context.GanttAsamalari.Remove(gantt);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Gantt aşaması başarıyla silindi!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Gantt aşaması bulunamadı!";
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = "Bu Gantt aşaması başka bir kullanıcı tarafından değiştirilmiş veya silinmiş. Lütfen sayfayı yenileyin ve tekrar deneyin.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gantt aşaması silinirken bir hata oluştu: " + ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetGanttStagesAsync(int projeId)
        {
            var ganttStages = await _context.GanttAsamalari
                .Where(g => g.ProjeID == projeId)
                .OrderBy(g => g.Sira)
                .Select(g => new { id = g.id, asama = g.Asama })
                .ToListAsync();

            return new JsonResult(ganttStages);
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
    }
}