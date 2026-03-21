namespace datn.Models
{
    public class ClassActivity
    {
        public int ClassId { get; set; }
        public int ActivityId { get; set; }

        public Class Class { get; set; }
        public Activity Activity { get; set; }
    }

}
