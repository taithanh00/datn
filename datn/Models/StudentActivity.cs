using System.ComponentModel.DataAnnotations;

namespace datn.Models
{
    public class StudentActivity
    {
        public int StudentId { get; set; }
        public Student Student { get; set; }

        public int ActivityId { get; set; }
        public Activity Activity { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }
    }
}
