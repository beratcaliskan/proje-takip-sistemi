namespace ProjeTakip.Models
{
    public class Sponsor
    {
        public int id { get; set; }
        public string BirimAd { get; set; } = string.Empty;
        public string SponsorAd { get; set; } = string.Empty;
        public string? IletisimBilgisi { get; set; }
        public string? Aciklama { get; set; }
    }
}