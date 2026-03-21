namespace datn.Models
{
    public class ParentStudent
    {
        public int ParentId { get; set; }
        public int StudentId { get; set; }
        public string? Relationship { get; set; }

        public Parent Parent { get; set; }
        public Student Student { get; set; }
    }
}
