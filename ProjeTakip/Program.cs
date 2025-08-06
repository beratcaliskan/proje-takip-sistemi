using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// Session servisleri ekleme
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Entity Framework DbContext ekleme
builder.Services.AddDbContext<ProjeTakipContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Seed data ekleme
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ProjeTakipContext>();
    SeedData(context);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Development ortamında HTTPS yönlendirmesini devre dışı bırak (yerel ağ erişimi için)
app.UseStaticFiles();

app.UseRouting();

// Session middleware ekleme
app.UseSession();

// Custom session kontrolü middleware
app.UseMiddleware<SessionMiddleware>();

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();

// Seed data fonksiyonu
static void SeedData(ProjeTakipContext context)
{
    // Veritabanının oluşturulduğundan emin ol
    context.Database.EnsureCreated();
    
    // Eğer kullanıcı yoksa test kullanıcıları ekle
    if (!context.Kullanicilar.Any())
    {
        context.Kullanicilar.AddRange(
            new ProjeTakip.Models.Kullanici
            {
                Kimlik = "admin",
                AdSoyad = "Admin Kullanıcı",
                Rol = 1 // Admin
            },
            new ProjeTakip.Models.Kullanici
            {
                Kimlik = "test",
                AdSoyad = "Test Kullanıcı",
                Rol = 2 // Proje Yöneticisi
            },
            new ProjeTakip.Models.Kullanici
            {
                Kimlik = "dev",
                AdSoyad = "Geliştirici Kullanıcı",
                Rol = 3 // Geliştirici
            }
        );
        
        context.SaveChanges();
    }
}
