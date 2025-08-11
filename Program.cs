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
    SeedDataAsync(context).Wait();
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
if (app.Environment.IsDevelopment())
{
    // Development ortamında static file caching'i devre dışı bırak
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            ctx.Context.Response.Headers.Append("Pragma", "no-cache");
            ctx.Context.Response.Headers.Append("Expires", "0");
        }
    });
}
else
{
    app.UseStaticFiles();
}

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
static async Task SeedDataAsync(ProjeTakipContext context)
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
                Sifre = "123456",
                Rol = 1 // Admin
            },
            new ProjeTakip.Models.Kullanici
            {
                Kimlik = "test",
                AdSoyad = "Test Kullanıcı",
                Sifre = "123456",
                Rol = 2 // Proje Yöneticisi
            },
            new ProjeTakip.Models.Kullanici
            {
                Kimlik = "dev",
                AdSoyad = "Geliştirici Kullanıcı",
                Sifre = "123456",
                Rol = 3 // Geliştirici
            }
        );
        
        context.SaveChanges();
    }
    
    // Test verilerini ekle (şimdilik devre dışı)
    // await ProjeTakip.TestDataSeeder.SeedTestDataAsync(context);
}
