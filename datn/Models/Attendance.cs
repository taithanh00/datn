namespace datn.Models
{
    public class Attendance
    {
        public int StudentId { get; set; }
        public DateOnly Date { get; set; }
        public string? Status { get; set; }
        public int? TakenBy { get; set; }

        public Student Student { get; set; }
        public Employee? Employee { get; set; }
    }
}
