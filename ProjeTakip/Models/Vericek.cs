using System;
using System.Collections.Generic;

namespace ProjeTakip.Models
{
    public class Vericek
    {
        public int id { get; set; }

        public int ProjeID { get; set; }
        public string ProjeAd { get; set; } = string.Empty;
        public string Mudurluk { get; set; } = string.Empty;
        public string Baskanlik { get; set; } = string.Empty;
        public string? Amac { get; set; }
        public string? Kapsam { get; set; }
        public decimal? Maliyet { get; set; }
        public string? Ekip { get; set; }
        public DateTime? bas { get; set; }
        public DateTime? bit { get; set; }
        public string? olcut { get; set; }
        public string? sponsor { get; set; }
        public string? sapma { get; set; }
        public string? Drm { get; set; }

        // Gantt özel alanlar
        public string? Asama { get; set; }
        public DateTime? Baslangic { get; set; }
        public DateTime? Bitis { get; set; }
        public int Gun { get; set; }
        public int Sira { get; set; }

        // Listeleme
        public int Adet { get; set; }
        public string? yil { get; set; }

        public List<Vericek>? projelist { get; set; }
        public List<Vericek>? detaylist { get; set; }
        public List<Vericek>? ganttlist { get; set; }
    }
}