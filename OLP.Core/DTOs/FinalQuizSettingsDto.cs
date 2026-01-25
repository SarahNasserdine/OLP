namespace OLP.Core.DTOs
{
    public class FinalQuizSettingsDto
    {
        public string Title { get; set; } = "Final Quiz";
        public int PassingScore { get; set; } = 70;
        public int TimeLimit { get; set; } = 0; // 0 => no limit
        public bool ShuffleQuestions { get; set; } = true;
        public bool AllowRetake { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }
}
