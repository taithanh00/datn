using datn.Models;

namespace datn.Controllers
{
    public class ParentChildrenViewModel
    {
        public Student Student { get; set; } = null!;
        public List<TodayLessonViewModel> TodayLessons { get; set; } = new();
    }

    public class TodayLessonViewModel
    {
        public string SubjectName { get; set; } = "";
        public string Time { get; set; } = "";
    }
}
