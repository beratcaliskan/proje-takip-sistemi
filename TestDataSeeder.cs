using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Models;

namespace ProjeTakip
{
    public static class TestDataSeeder
    {
        public static async Task SeedTestDataAsync(ProjeTakipContext context)
        {
            // Test için Gantt aşamaları ekle
            if (!await context.GanttAsamalari.AnyAsync())
            {
                var ganttAsamalari = new List<Gantt>
                {
                    new Gantt { id = 1, ProjeID = 1, Asama = "Planlama", Sira = 1 },
                    new Gantt { id = 2, ProjeID = 1, Asama = "Analiz", Sira = 2 },
                    new Gantt { id = 3, ProjeID = 1, Asama = "Geliştirme", Sira = 3 },
                    new Gantt { id = 4, ProjeID = 1, Asama = "Test", Sira = 4 },
                    new Gantt { id = 5, ProjeID = 1, Asama = "Dağıtım", Sira = 5 }
                };
                
                await context.GanttAsamalari.AddRangeAsync(ganttAsamalari);
                await context.SaveChangesAsync();
            }

            // Test için ilerleme verileri ekle
            if (!await context.Ilerlemeler.AnyAsync())
            {
                var ilerlemeler = new List<Ilerleme>
                {
                    new Ilerleme
                    {
                        ProjeID = 1,
                        GanttID = 1,
                        IlerlemeTanimi = "Proje planlaması tamamlandı",
                        TamamlanmaYuzdesi = 100,
                        IlerlemeTarihi = DateTime.Now.AddDays(-30),
                        Aciklama = "İlk aşama başarıyla tamamlandı"
                    },
                    new Ilerleme
                    {
                        ProjeID = 1,
                        GanttID = 2,
                        IlerlemeTanimi = "Analiz çalışmaları devam ediyor",
                        TamamlanmaYuzdesi = 60,
                        IlerlemeTarihi = DateTime.Now.AddDays(-15),
                        Aciklama = "Analiz aşamasında ilerleme kaydedildi"
                    },
                    new Ilerleme
                    {
                        ProjeID = 1,
                        GanttID = 3,
                        IlerlemeTanimi = "Geliştirme başladı",
                        TamamlanmaYuzdesi = 25,
                        IlerlemeTarihi = DateTime.Now.AddDays(-5),
                        Aciklama = "Geliştirme aşamasına geçildi"
                    }
                };
                
                await context.Ilerlemeler.AddRangeAsync(ilerlemeler);
                await context.SaveChangesAsync();
            }
        }
    }
}