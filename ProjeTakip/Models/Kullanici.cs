namespace ProjeTakip.Models
{
    public class Kullanici
    {
        public int id { get; set; }
        public string AdSoyad { get; set; } = string.Empty;
        public string Kimlik { get; set; } = string.Empty; // Personel ID
        public int Rol { get; set; }
    }
}