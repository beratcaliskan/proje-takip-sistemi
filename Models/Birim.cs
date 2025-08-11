namespace ProjeTakip.Models
{
    public class Birim
    {
        public int id { get; set; }
        public string BirimAd { get; set; } = string.Empty;
        
        // Navigation property
        public virtual ICollection<Proje>? Projeler { get; set; }
    }
}