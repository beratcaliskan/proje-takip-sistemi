using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjeTakip.Data;
using ProjeTakip.Models;

namespace ProjeTakip.Pages.SystemLogs
{
    public class IndexModel : PageModel
    {
        private readonly ProjeTakipContext _context;

        public IndexModel(ProjeTakipContext context)
        {
            _context = context;
        }

        public List<SystemLog> SystemLogs { get; set; } = new List<SystemLog>();
        public int TotalLogs { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalPages { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
        public string LogTypeFilter { get; set; } = string.Empty;
        public string ExecutorFilter { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public async Task<IActionResult> OnGetAsync(int pageNumber = 1, string searchTerm = "", string logTypeFilter = "", 
            string executorFilter = "", DateTime? startDate = null, DateTime? endDate = null)
        {
            // Sadece rol 1 (Admin) erişebilir
            var userRole = HttpContext.Session.GetInt32("UserRole");
            if (userRole != 1)
            {
                return RedirectToPage("/Index");
            }
            
            PageNumber = pageNumber;
            SearchTerm = searchTerm ?? string.Empty;
            LogTypeFilter = logTypeFilter ?? string.Empty;
            ExecutorFilter = executorFilter ?? string.Empty;
            StartDate = startDate;
            EndDate = endDate;

            var query = _context.SystemLogs.AsQueryable();

            // Filtreleme
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(l => l.LogContent.Contains(SearchTerm) || l.Executor.Contains(SearchTerm));
            }

            if (!string.IsNullOrEmpty(LogTypeFilter))
            {
                query = query.Where(l => l.LogType == LogTypeFilter);
            }

            if (!string.IsNullOrEmpty(ExecutorFilter))
            {
                query = query.Where(l => l.Executor.Contains(ExecutorFilter));
            }

            if (StartDate.HasValue)
            {
                query = query.Where(l => l.CreatedAt >= StartDate.Value);
            }

            if (EndDate.HasValue)
            {
                query = query.Where(l => l.CreatedAt <= EndDate.Value.AddDays(1));
            }

            TotalLogs = await query.CountAsync();
            TotalPages = (int)Math.Ceiling((double)TotalLogs / PageSize);

            SystemLogs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
                
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var log = await _context.SystemLogs.FindAsync(id);
            if (log != null)
            {
                _context.SystemLogs.Remove(log);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostClearOldLogsAsync(int days = 30)
        {
            var cutoffDate = DateTime.Now.AddDays(-days);
            var oldLogs = await _context.SystemLogs
                .Where(l => l.CreatedAt < cutoffDate)
                .ToListAsync();

            _context.SystemLogs.RemoveRange(oldLogs);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}