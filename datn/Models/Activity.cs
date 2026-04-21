namespace datn.Models
{
    public class Activity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateOnly? Date { get; set; }
        public int? LocationId { get; set; }
        public int? OrganizerId { get; set; }

        public Location? Location { get; set; }
        public Employee? Organizer { get; set; }
        public ICollection<ClassActivity> ClassActivities { get; set; } = new List<ClassActivity>();
        public ICollection<StudentActivity> StudentActivities { get; set; } = new List<StudentActivity>();
    }
}
