namespace datn.Services
{
    public static class TeacherRoleDisplay
    {
        public const string LeadTeacher = "Giáo viên phụ trách";

        public static string ToDisplayName(string? roleInClass)
        {
            return LeadTeacher;
        }

        public static string NormalizeForStorage(string? roleInClass)
        {
            return LeadTeacher;
        }
    }
}
