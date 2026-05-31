namespace datn.Services
{
    public class ClassCoverageOptions
    {
        public const string SectionName = "ClassCoverage";

        public decimal SoloCoverageBonusAmount { get; set; } = 50_000m;
        public bool RequirePresentCheckIn { get; set; } = true;
    }
}
