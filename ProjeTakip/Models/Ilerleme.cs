using System;

namespace ProjeTakip.Models
{
    public class Ilerleme
    {
        public int id { get; set; }
        public int ProjeID { get; set; }
        public int GanttID { get; set; }
        public string IlerlemeTanimi { get; set; } = string.Empty;
        public int TamamlanmaYuzdesi { get; set; }
        public DateTime IlerlemeTarihi { get; set; }
        public string? Aciklama { get; set; }
        public int? KullaniciID { get; set; } // Kim ekledi
        
        // Navigation properties
        public Proje? Proje { get; set; }
        public Gantt? GanttAsama { get; set; }
        public Kullanici? EkleyenKullanici { get; set; }
    }
}