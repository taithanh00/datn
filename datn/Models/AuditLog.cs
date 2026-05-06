using System;
using System.ComponentModel.DataAnnotations;

namespace datn.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        
        public string? UserId { get; set; }
        
        [StringLength(100)]
        public string? UserName { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty; // Added, Modified, Deleted
        
        [Required]
        [StringLength(100)]
        public string EntityName { get; set; } = string.Empty; // Table name
        
        [StringLength(100)]
        public string? EntityId { get; set; }
        
        public string? OldValues { get; set; } // JSON
        
        public string? NewValues { get; set; } // JSON
        
        [StringLength(50)]
        public string? IpAddress { get; set; }
        
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
