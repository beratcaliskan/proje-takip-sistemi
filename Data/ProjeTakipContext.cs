using Microsoft.EntityFrameworkCore;
using ProjeTakip.Models;

namespace ProjeTakip.Data
{
    public class ProjeTakipContext : DbContext
    {
        public DbSet<Proje> Projeler { get; set; }
        public DbSet<Gantt> GanttAsamalari { get; set; }
        public DbSet<Kullanici> Kullanicilar { get; set; }
        public DbSet<Sponsor> Sponsorler { get; set; }
        public DbSet<Birim> Birimler { get; set; }
        public DbSet<Ilerleme> Ilerlemeler { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }

        public ProjeTakipContext(DbContextOptions<ProjeTakipContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Proje - Gantt ilişkisi
            modelBuilder.Entity<Gantt>()
                .HasOne(g => g.Proje)
                .WithMany(p => p.GanttAsamalari)
                .HasForeignKey(g => g.ProjeID);

            // İlerleme - Proje ilişkisi
            modelBuilder.Entity<Ilerleme>()
                .HasOne(i => i.Proje)
                .WithMany()
                .HasForeignKey(i => i.ProjeID);

            // İlerleme - Gantt ilişkisi
            modelBuilder.Entity<Ilerleme>()
                .HasOne(i => i.GanttAsama)
                .WithMany()
                .HasForeignKey(i => i.GanttID);

            // İlerleme - Kullanici ilişkisi
            modelBuilder.Entity<Ilerleme>()
                .HasOne(i => i.EkleyenKullanici)
                .WithMany()
                .HasForeignKey(i => i.KullaniciID);

            // Proje - Birim ilişkisi
            modelBuilder.Entity<Proje>()
                .HasOne<Birim>()
                .WithMany(b => b.Projeler)
                .HasForeignKey(p => p.BirimId);

            base.OnModelCreating(modelBuilder);
        }
    }
}