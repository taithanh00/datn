namespace datn.Models
{
    public class Parent
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }

        public Account Account { get; set; }
        public ICollection<ParentStudent> ParentStudents { get; set; }
    }
}
