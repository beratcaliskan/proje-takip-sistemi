using System;
using System.Collections.Generic;

namespace ProjeTakip.Models
{
    public class Proje
    {
        public int ProjeID { get; set; }
        public string ProjeAd { get; set; } = string.Empty;
        public string Mudurluk { get; set; } = string.Empty;
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
        public int Durum { get; set; }
        public int personel { get; set; }

        public ICollection<Gantt>? GanttAsamalari { get; set; }
        
        // Computed properties for reports
        public string DurumAdi => Durum switch
        {
            1 => "Onay Bekliyor",
            2 => "Planlama",
            3 => "Devam Ediyor",
            4 => "Tamamlandı",
            5 => "İptal Edildi",
            _ => "Bilinmiyor"
        };
        
        public string MaliyetFormatli => Maliyet?.ToString("C", new System.Globalization.CultureInfo("tr-TR")) ?? "Belirtilmemiş";
        
        public string BaslangicTarihi => bas?.ToString("dd.MM.yyyy") ?? "Belirtilmemiş";
        
        public string BitisTarihi => bit?.ToString("dd.MM.yyyy") ?? "Belirtilmemiş";
    }
}