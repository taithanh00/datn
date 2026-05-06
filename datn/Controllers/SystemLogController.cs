using datn.Data;
using datn.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace datn.Controllers
{
    [Authorize(Roles = "Manager")]
    [Route("Manager/[controller]")]
    public class SystemLogController : BaseController
    {
        public SystemLogController(AppDbContext context) : base(context)
        {
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Nhật ký hệ thống";
            return View("~/Views/Dashboard/Manager/SystemLogs.cshtml");
        }

        [HttpGet("GetData")]
        public async Task<IActionResult> GetData(
            int page = 1, 
            int pageSize = 50, 
            string? search = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? entityName = null,
            string? userName = null,
            string? logAction = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            // Text search
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(l => 
                    l.UserName.Contains(search) || 
                    l.EntityName.Contains(search) || 
                    l.Action.Contains(search) ||
                    l.EntityId.Contains(search)
                );
            }

            // Advanced Filters
            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;
                query = query.Where(l => l.CreatedAtUtc >= start);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(l => l.CreatedAtUtc <= end);
            }

            if (!string.IsNullOrEmpty(entityName))
            {
                query = query.Where(l => l.EntityName == entityName);
            }

            if (!string.IsNullOrEmpty(userName))
            {
                query = query.Where(l => l.UserName == userName);
            }

            if (!string.IsNullOrEmpty(logAction))
            {
                query = query.Where(l => l.Action == logAction);
            }

            var totalItems = await query.CountAsync();
            var logs = await query
                .OrderByDescending(l => l.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Json(new
            {
                success = true,
                data = logs,
                totalItems = totalItems,
                totalPages = (int)System.Math.Ceiling((double)totalItems / pageSize)
            });
        }




        [HttpGet("GetFilterOptions")]
        public async Task<IActionResult> GetFilterOptions()
        {
            var entities = await _context.AuditLogs
                .Select(l => l.EntityName)
                .Distinct()
                .OrderBy(e => e)
                .ToListAsync();

            var users = await _context.AuditLogs
                .Where(l => l.UserName != null)
                .Select(l => l.UserName)
                .Distinct()
                .OrderBy(u => u)
                .ToListAsync();

            var actions = await _context.AuditLogs
                .Select(l => l.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            return Json(new
            {
                entities,
                users,
                actions
            });
        }
    }
}
