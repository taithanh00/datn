namespace datn.Services
{
    public static class TeacherRoleDisplay
    {
        public static string ToDisplayName(string? roleInClass)
        {
            if (string.IsNullOrWhiteSpace(roleInClass))
                return "Giáo viên phụ trách";

            var normalized = RemoveDiacritics(roleInClass).Trim().ToLowerInvariant();

            if (normalized.Contains("chu nhiem") ||
                normalized.Contains("gvcn") ||
                normalized.Contains("homeroom") ||
                normalized.Contains("lead"))
            {
                return "Giáo viên phụ trách";
            }

            return roleInClass.Trim();
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();

            foreach (var c in normalized)
            {
                var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }
    }
}
