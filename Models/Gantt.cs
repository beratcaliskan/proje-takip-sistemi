using System;

namespace ProjeTakip.Models
{
    public class Gantt
    {
        public int id { get; set; }
        public int ProjeID { get; set; }
        public string Asama { get; set; } = string.Empty;
        public DateTime? Baslangic { get; set; }
        public DateTime? Bitis { get; set; }
        public int Gun { get; set; }
        public int Sira { get; set; }

        public Proje? Proje { get; set; }
    }
}