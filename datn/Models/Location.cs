using System.Diagnostics;

namespace datn.Models
{
    public class Location
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        public ICollection<Activity> Activities { get; set; }
    }
}
