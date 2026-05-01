namespace datn.Models
{
    public class Subject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public decimal? FeeAmount { get; set; } // Phí môn năng khiếu nếu có

        public ICollection<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();
        public ICollection<TuitionDetail> TuitionDetails { get; set; } = new List<TuitionDetail>();
    }
}
