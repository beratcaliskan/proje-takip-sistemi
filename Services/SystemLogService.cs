using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Models;

namespace ProjeTakip.Services
{
    public interface ISystemLogService
    {
        Task LogAsync(string logType, string logContent, string executor, string? additionalInfo = null, string? ipAddress = null, string? userAgent = null);
        Task LogUserLoginAsync(string userKimlik, string userName, string ipAddress, string userAgent);
        Task LogUserLogoutAsync(string userKimlik, string userName, string ipAddress);
        Task LogProjectAddedAsync(string projectName, string executor, string ipAddress);
        Task LogProjectUpdatedAsync(string projectName, string executor, string ipAddress);
        Task LogProjectDeletedAsync(string projectName, string executor, string ipAddress);
        Task LogUserAddedAsync(string newUserKimlik, string executor, string ipAddress);
        Task LogUserUpdatedAsync(string userKimlik, string executor, string ipAddress);
        Task LogUserDeletedAsync(string userKimlik, string executor, string ipAddress);
        Task LogUnitAddedAsync(string unitName, string executor, string ipAddress);
        Task LogUnitUpdatedAsync(string unitName, string executor, string ipAddress);
        Task LogUnitDeletedAsync(string unitName, string executor, string ipAddress);
        Task LogSponsorAddedAsync(string sponsorName, string executor, string ipAddress);
        Task LogSponsorUpdatedAsync(string sponsorName, string executor, string ipAddress);
        Task LogSponsorDeletedAsync(string sponsorName, string executor, string ipAddress);
    }

    public class SystemLogService : ISystemLogService
    {
        private readonly ProjeTakipContext _context;

        public SystemLogService(ProjeTakipContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string logType, string logContent, string executor, string? additionalInfo = null, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                var log = new SystemLog
                {
                    LogType = logType,
                    LogContent = logContent,
                    Executor = executor,
                    AdditionalInfo = additionalInfo,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    CreatedAt = DateTime.Now
                };

                _context.SystemLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log kaydı başarısız olursa sessizce devam et
                // Gerçek uygulamada bu durumu ayrı bir logging mekanizmasıyla kaydetmek gerekir
                Console.WriteLine($"System log kaydedilemedi: {ex.Message}");
            }
        }

        public async Task LogUserLoginAsync(string userKimlik, string userName, string ipAddress, string userAgent)
        {
            await LogAsync("Kullanıcı Girişi", $"{userName} ({userKimlik}) sisteme giriş yaptı", userName, null, ipAddress, userAgent);
        }

        public async Task LogUserLogoutAsync(string userKimlik, string userName, string ipAddress)
        {
            await LogAsync("Kullanıcı Çıkışı", $"{userName} ({userKimlik}) sistemden çıkış yaptı", userName, null, ipAddress, null);
        }

        public async Task LogProjectAddedAsync(string projectName, string executor, string ipAddress)
        {
            await LogAsync("Proje Eklendi", $"'{projectName}' adlı proje eklendi", executor, null, ipAddress, null);
        }

        public async Task LogProjectUpdatedAsync(string projectName, string executor, string ipAddress)
        {
            await LogAsync("Proje Güncellendi", $"'{projectName}' adlı proje güncellendi", executor, null, ipAddress, null);
        }

        public async Task LogProjectDeletedAsync(string projectName, string executor, string ipAddress)
        {
            await LogAsync("Proje Silindi", $"'{projectName}' adlı proje silindi", executor, null, ipAddress, null);
        }

        public async Task LogUserAddedAsync(string newUserKimlik, string executor, string ipAddress)
        {
            await LogAsync("Kullanıcı Eklendi", $"'{newUserKimlik}' kimlikli kullanıcı eklendi", executor, null, ipAddress, null);
        }

        public async Task LogUserUpdatedAsync(string userKimlik, string executor, string ipAddress)
        {
            await LogAsync("Kullanıcı Güncellendi", $"'{userKimlik}' kimlikli kullanıcı güncellendi", executor, null, ipAddress, null);
        }

        public async Task LogUserDeletedAsync(string userKimlik, string executor, string ipAddress)
        {
            await LogAsync("Kullanıcı Silindi", $"'{userKimlik}' kimlikli kullanıcı silindi", executor, null, ipAddress, null);
        }

        public async Task LogUnitAddedAsync(string unitName, string executor, string ipAddress)
        {
            await LogAsync("Birim Eklendi", $"'{unitName}' adlı birim eklendi", executor, null, ipAddress, null);
        }

        public async Task LogUnitUpdatedAsync(string unitName, string executor, string ipAddress)
        {
            await LogAsync("Birim Güncellendi", $"'{unitName}' adlı birim güncellendi", executor, null, ipAddress, null);
        }

        public async Task LogUnitDeletedAsync(string unitName, string executor, string ipAddress)
        {
            await LogAsync("Birim Silindi", $"'{unitName}' adlı birim silindi", executor, null, ipAddress, null);
        }

        public async Task LogSponsorAddedAsync(string sponsorName, string executor, string ipAddress)
        {
            await LogAsync("Sponsor Eklendi", $"'{sponsorName}' adlı sponsor eklendi", executor, null, ipAddress, null);
        }

        public async Task LogSponsorUpdatedAsync(string sponsorName, string executor, string ipAddress)
        {
            await LogAsync("Sponsor Güncellendi", $"'{sponsorName}' adlı sponsor güncellendi", executor, null, ipAddress, null);
        }

        public async Task LogSponsorDeletedAsync(string sponsorName, string executor, string ipAddress)
        {
            await LogAsync("Sponsor Silindi", $"'{sponsorName}' adlı sponsor silindi", executor, null, ipAddress, null);
        }
    }
}