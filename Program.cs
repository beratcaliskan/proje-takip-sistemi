using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Middleware;
using ProjeTakip.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// API için Swagger/OpenAPI ekleme
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Proje Takip API",
        Version = "v1",
        Description = "Proje Takip Sistemi için RESTful API"
    });
    
    // Bearer token authentication için
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// CORS ekleme (mobil uygulamalar için)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMobileApps", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

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

// SystemLogService ekleme
builder.Services.AddScoped<ISystemLogService, SystemLogService>();
builder.Services.AddScoped<SystemLogService>();

var app = builder.Build();

// Seed data ekleme
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ProjeTakipContext>();
    SeedDataAsync(context).Wait();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Development ortamında Swagger'ı etkinleştir
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Proje Takip API v1");
        c.RoutePrefix = "api-docs"; // Swagger UI'ya /api-docs üzerinden erişim
    });
}
else
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

// CORS middleware ekleme (mobil uygulamalar için)
app.UseCors("AllowMobileApps");

// API Authentication middleware ekleme (mobil API için)
app.UseMiddleware<ProjeTakip.Middleware.ApiAuthenticationMiddleware>();

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
