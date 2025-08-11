using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Models;

namespace ProjeTakip.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SystemLogsController : ControllerBase
    {
        private readonly ProjeTakipContext _context;

        public SystemLogsController(ProjeTakipContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetSystemLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? logType = null,
            [FromQuery] string? executor = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var query = _context.SystemLogs.AsQueryable();

                // Filtreler
                if (!string.IsNullOrEmpty(logType))
                {
                    query = query.Where(s => s.LogType.Contains(logType));
                }

                if (!string.IsNullOrEmpty(executor))
                {
                    query = query.Where(s => s.Executor.Contains(executor));
                }

                if (startDate.HasValue)
                {
                    query = query.Where(s => s.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(s => s.CreatedAt <= endDate.Value);
                }

                // Toplam kayıt sayısı
                var totalCount = await query.CountAsync();

                // Sayfalama
                var logs = await query
                    .OrderByDescending(s => s.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new
                    {
                        id = s.Id,
                        logType = s.LogType,
                        logContent = s.LogContent,
                        executor = s.Executor,
                        ipAddress = s.IpAddress,
                        userAgent = s.UserAgent,
                        additionalInfo = s.AdditionalInfo,
                        createdAt = s.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = logs,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSystemLog(int id)
        {
            try
            {
                var log = await _context.SystemLogs.FindAsync(id);
                if (log == null)
                {
                    return NotFound(new { success = false, message = "Log kaydı bulunamadı" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = log.Id,
                        logType = log.LogType,
                        logContent = log.LogContent,
                        executor = log.Executor,
                        ipAddress = log.IpAddress,
                        userAgent = log.UserAgent,
                        additionalInfo = log.AdditionalInfo,
                        createdAt = log.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetLogStatistics([FromQuery] int days = 30)
        {
            try
            {
                var startDate = DateTime.Now.AddDays(-days);
                var logs = await _context.SystemLogs
                    .Where(s => s.CreatedAt >= startDate)
                    .ToListAsync();

                var statistics = new
                {
                    toplamLog = logs.Count,
                    kullaniciGirisleri = logs.Count(l => l.LogType == "Kullanıcı Girişi"),
                    kullaniciCikislari = logs.Count(l => l.LogType == "Kullanıcı Çıkışı"),
                    projeIslemleri = logs.Count(l => l.LogType.Contains("Proje")),
                    kullaniciIslemleri = logs.Count(l => l.LogType.Contains("Kullanıcı") && !l.LogType.Contains("Giriş") && !l.LogType.Contains("Çıkış")),
                    ilerlemeler = logs.Count(l => l.LogType.Contains("İlerleme")),
                    gunlukDagilim = logs
                        .GroupBy(l => l.CreatedAt.Date)
                        .Select(g => new
                        {
                            tarih = g.Key,
                            sayi = g.Count()
                        })
                        .OrderBy(x => x.tarih)
                        .ToList(),
                    logTipDagilimi = logs
                        .GroupBy(l => l.LogType)
                        .Select(g => new
                        {
                            logType = g.Key,
                            sayi = g.Count()
                        })
                        .OrderByDescending(x => x.sayi)
                        .Take(10)
                        .ToList(),
                    enAktifKullanicilar = logs
                        .Where(l => !string.IsNullOrEmpty(l.Executor) && l.Executor != "System")
                        .GroupBy(l => l.Executor)
                        .Select(g => new
                        {
                            kullanici = g.Key,
                            aktiviteSayisi = g.Count()
                        })
                        .OrderByDescending(x => x.aktiviteSayisi)
                        .Take(10)
                        .ToList()
                };

                return Ok(new { success = true, data = statistics });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentLogs([FromQuery] int count = 10)
        {
            try
            {
                var recentLogs = await _context.SystemLogs
                    .Where(s => s.LogType != "Kullanıcı Girişi" && s.LogType != "Kullanıcı Çıkışı")
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(count)
                    .Select(s => new
                    {
                        id = s.Id,
                        logType = s.LogType,
                        logContent = s.LogContent,
                        executor = s.Executor,
                        createdAt = s.CreatedAt,
                        timeAgo = GetTimeAgo(s.CreatedAt)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = recentLogs
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpGet("user-activities/{userId}")]
        public async Task<IActionResult> GetUserActivities(int userId, [FromQuery] int days = 30)
        {
            try
            {
                // Kullanıcı kontrolü
                var kullanici = await _context.Kullanicilar.FindAsync(userId);
                if (kullanici == null)
                {
                    return NotFound(new { success = false, message = "Kullanıcı bulunamadı" });
                }

                var startDate = DateTime.Now.AddDays(-days);
                var userLogs = await _context.SystemLogs
                    .Where(s => s.Executor == kullanici.AdSoyad && s.CreatedAt >= startDate)
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new
                    {
                        id = s.Id,
                        logType = s.LogType,
                        logContent = s.LogContent,
                        ipAddress = s.IpAddress,
                        createdAt = s.CreatedAt,
                        timeAgo = GetTimeAgo(s.CreatedAt)
                    })
                    .ToListAsync();

                var statistics = new
                {
                    kullanici = new
                    {
                        id = kullanici.id,
                        adSoyad = kullanici.AdSoyad,
                        kimlik = kullanici.Kimlik
                    },
                    toplamAktivite = userLogs.Count,
                    sonGiris = userLogs.FirstOrDefault(l => l.logType == "Kullanıcı Girişi")?.createdAt,
                    aktiviteler = userLogs
                };

                return Ok(new { success = true, data = statistics });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpDelete("clear-old-logs")]
        public async Task<IActionResult> ClearOldLogs([FromQuery] int days = 90)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-days);
                var oldLogs = await _context.SystemLogs
                    .Where(s => s.CreatedAt < cutoffDate)
                    .ToListAsync();

                if (oldLogs.Any())
                {
                    _context.SystemLogs.RemoveRange(oldLogs);
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    success = true,
                    message = $"{oldLogs.Count} adet eski log kaydı silindi",
                    deletedCount = oldLogs.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sunucu hatası: " + ex.Message });
            }
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Az önce";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} dakika önce";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} saat önce";
            if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays} gün önce";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} ay önce";
            
            return $"{(int)(timeSpan.TotalDays / 365)} yıl önce";
        }
    }
}