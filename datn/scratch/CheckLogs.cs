using datn.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace datn.Scratch
{
    public class CheckLogs
    {
        public static void Run(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            var count = context.AuditLogs.Count();
            Console.WriteLine($"Total Audit Logs: {count}");
            
            var lastLogs = context.AuditLogs.OrderByDescending(l => l.CreatedAtUtc).Take(5).ToList();
            foreach(var log in lastLogs)
            {
                Console.WriteLine($"Log: {log.Action} on {log.EntityName} at {log.CreatedAtUtc}");
            }
        }
    }
}
