namespace OLP.Core.DTOs
{
    public class QuizAvgDto
    {
        public int QuizId { get; set; }
        public string Title { get; set; } = "";
        public double AvgScore { get; set; }
        public int AttemptsCount { get; set; }
    }
}
