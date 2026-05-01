using System.Diagnostics;

namespace datn.Models
{
    public class Location
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<Activity> Activities { get; set; }
    }
}
