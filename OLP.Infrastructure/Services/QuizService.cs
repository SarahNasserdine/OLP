using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using OLP.Core.DTOs;
using OLP.Core.Entities;
using OLP.Core.Enums;
using OLP.Core.Interfaces;

namespace OLP.Infrastructure.Services
{
    public class QuizService : IQuizService
    {
        private readonly IQuizRepository _quizRepo;
        private readonly IQuizAttemptRepository _attemptRepo;
        private readonly IQuizAttemptAnswerRepository _attemptAnswerRepo;
        private readonly IQuestionRepository _questionRepo;

        public QuizService(
            IQuizRepository quizRepo,
            IQuizAttemptRepository attemptRepo,
            IQuizAttemptAnswerRepository attemptAnswerRepo,
            IQuestionRepository questionRepo)
        {
            _quizRepo = quizRepo;
            _attemptRepo = attemptRepo;
            _attemptAnswerRepo = attemptAnswerRepo;
            _questionRepo = questionRepo;
        }

        public async Task<QuizAttempt> StartAttemptAsync(int userId, int quizId)
        {
            var quiz = await _quizRepo.GetByIdAsync(quizId);
            if (quiz == null)
                throw new Exception("Quiz not found.");

            var userAttempts = await _attemptRepo.GetByUserIdAsync(userId);
            int attemptNumber = userAttempts.Count(a => a.QuizId == quizId) + 1;

            if (!quiz.AllowRetake && !quiz.IsFinal && userAttempts.Any(a => a.QuizId == quizId))
                throw new Exception("Retake is not allowed for this quiz.");

            var attempt = new QuizAttempt
            {
                QuizId = quizId,
                UserId = userId,
                AttemptNumber = attemptNumber,
                AttemptDate = DateTime.UtcNow,
                StartedAt = DateTime.UtcNow,
                Score = 0
            };

            await _attemptRepo.AddAsync(attempt);
            await _attemptRepo.SaveChangesAsync();
            return attempt;
        }

        public async Task<QuizAttempt> StartAttemptWithSelectedQuestionsAsync(
            int userId,
            int quizId,
            IEnumerable<int> questionIds)
        {
            var quiz = await _quizRepo.GetByIdAsync(quizId);
            if (quiz == null)
                throw new Exception("Quiz not found.");

            var userAttempts = await _attemptRepo.GetByUserIdAsync(userId);
            int attemptNumber = userAttempts.Count(a => a.QuizId == quizId) + 1;

            if (!quiz.AllowRetake && !quiz.IsFinal && userAttempts.Any(a => a.QuizId == quizId))
                throw new Exception("Retake is not allowed for this quiz.");

            var selectedIds = questionIds?.Distinct().ToList() ?? new List<int>();

            var attempt = new QuizAttempt
            {
                QuizId = quizId,
                UserId = userId,
                AttemptNumber = attemptNumber,
                AttemptDate = DateTime.UtcNow,
                StartedAt = DateTime.UtcNow,
                Score = 0,
                SelectedQuestionIdsJson = JsonSerializer.Serialize(selectedIds)
            };

            await _attemptRepo.AddAsync(attempt);
            await _attemptRepo.SaveChangesAsync();
            return attempt;
        }

        public async Task<QuizAttempt> SubmitAttemptAsync(int userId, SubmitQuizDto dto)
        {
            var quiz = await _quizRepo.GetByIdWithQuestionsAsync(dto.QuizId);
            if (quiz == null)
                throw new Exception("Quiz not found.");

            QuizAttempt? attempt = null;
            if (dto.AttemptId.HasValue)
            {
                attempt = await _attemptRepo.GetByIdAsync(dto.AttemptId.Value);
                if (attempt == null)
                    throw new Exception("Attempt not found.");
                if (attempt.UserId != userId || attempt.QuizId != dto.QuizId)
                    throw new Exception("Invalid attempt.");
                if (attempt.SubmittedAt.HasValue)
                    throw new Exception("Attempt already submitted.");
            }

            if (attempt == null)
            {
                var userAttempts = await _attemptRepo.GetByUserIdAsync(userId);
                int attemptNumber = userAttempts.Count(a => a.QuizId == dto.QuizId) + 1;

                if (!quiz.AllowRetake && userAttempts.Any(a => a.QuizId == dto.QuizId))
                    throw new Exception("Retake is not allowed for this quiz.");

                attempt = new QuizAttempt
                {
                    QuizId = dto.QuizId,
                    UserId = userId,
                    AttemptNumber = attemptNumber,
                    AttemptDate = DateTime.UtcNow,
                    StartedAt = DateTime.UtcNow,
                    Score = 0
                };

                await _attemptRepo.AddAsync(attempt);
                await _attemptRepo.SaveChangesAsync();
            }

            if (quiz.TimeLimit.HasValue)
            {
                var elapsed = DateTime.UtcNow - attempt.StartedAt;
                if (elapsed.TotalMinutes > quiz.TimeLimit.Value)
                {
                    attempt.SubmittedAt = DateTime.UtcNow;
                    attempt.Score = 0;
                    await _attemptRepo.SaveChangesAsync();
                    throw new Exception("Time limit exceeded.");
                }
            }

            var selectedQuestionIds = ParseSelectedQuestionIds(attempt.SelectedQuestionIdsJson);
            List<Question> questions;

            if (selectedQuestionIds.Count > 0)
            {
                questions = (await _questionRepo.GetByIdsWithAnswersAsync(selectedQuestionIds)).ToList();
            }
            else
            {
                questions = quiz.Questions?.ToList() ?? new List<Question>();
            }

            var submittedByQuestion = (dto.Answers ?? new List<SubmitAnswerDto>())
                .GroupBy(a => a.QuestionId)
                .ToDictionary(g => g.Key, g => g.Last());

            if (selectedQuestionIds.Count > 0)
            {
                submittedByQuestion = submittedByQuestion
                    .Where(kvp => selectedQuestionIds.Contains(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            int earnedPoints = 0;
            int totalPoints = questions.Sum(q => Math.Max(0, q.Points));

            foreach (var q in questions)
            {
                submittedByQuestion.TryGetValue(q.Id, out var submitted);

                var attemptAnswer = new QuizAttemptAnswer
                {
                    QuizAttemptId = attempt.Id,
                    QuestionId = q.Id,
                    CreatedAt = DateTime.UtcNow
                };

                bool isCorrect = false;

                if (q.QuestionType == QuestionType.MCQ || q.QuestionType == QuestionType.TF)
                {
                    if (submitted?.AnswerId != null)
                    {
                        var selected = (q.Answers ?? new List<Answer>())
                            .FirstOrDefault(a => a.Id == submitted.AnswerId.Value);

                        attemptAnswer.AnswerId = submitted.AnswerId.Value;
                        isCorrect = selected?.IsCorrect == true;
                    }
                }
                else if (q.QuestionType == QuestionType.MSQ)
                {
                    var selectedIds = (submitted?.AnswerIds ?? new List<int>())
                        .Distinct()
                        .OrderBy(x => x)
                        .ToList();

                    var correctIds = (q.Answers ?? new List<Answer>())
                        .Where(a => a.IsCorrect)
                        .Select(a => a.Id)
                        .OrderBy(x => x)
                        .ToList();

                    isCorrect = correctIds.SequenceEqual(selectedIds);
                    attemptAnswer.SelectedAnswerIdsJson = JsonSerializer.Serialize(selectedIds);
                }
                else if (q.QuestionType == QuestionType.ShortAnswer)
                {
                    attemptAnswer.GivenTextAnswer = submitted?.GivenTextAnswer;
                    isCorrect = false;
                }

                attemptAnswer.IsCorrect = isCorrect;

                await _attemptAnswerRepo.AddAsync(attemptAnswer);

                if (isCorrect)
                    earnedPoints += Math.Max(0, q.Points);
            }

            await _attemptAnswerRepo.SaveChangesAsync();

            double percent = totalPoints == 0 ? 0 : (earnedPoints * 100.0) / totalPoints;
            attempt.Score = (int)Math.Round(percent, MidpointRounding.AwayFromZero);
            attempt.SubmittedAt = DateTime.UtcNow;

            await _attemptRepo.SaveChangesAsync();

            return attempt;
        }

        public async Task<QuizReviewDto> GetAttemptReviewAsync(int attemptId, int userId)
        {
            var attempt = await _attemptRepo.GetByIdAsync(attemptId);
            if (attempt == null)
                throw new Exception("Attempt not found.");

            if (attempt.UserId != userId)
                throw new Exception("Unauthorized attempt review.");

            var quiz = await _quizRepo.GetByIdWithQuestionsAsync(attempt.QuizId);
            if (quiz == null)
                throw new Exception("Quiz not found.");

            var attemptAnswers = (await _attemptAnswerRepo.GetByAttemptIdAsync(attemptId)).ToList();
            var selectedQuestionIds = ParseSelectedQuestionIds(attempt.SelectedQuestionIdsJson);
            List<Question> questions;

            if (selectedQuestionIds.Count > 0)
            {
                questions = (await _questionRepo.GetByIdsWithAnswersAsync(selectedQuestionIds)).ToList();
            }
            else
            {
                questions = quiz.Questions?.ToList() ?? new List<Question>();
            }

            var review = new QuizReviewDto
            {
                AttemptId = attempt.Id,
                QuizId = quiz.Id,
                Score = attempt.Score,
                AttemptNumber = attempt.AttemptNumber,
                AttemptDate = attempt.AttemptDate,
                Questions = questions.Select(q =>
                {
                    var aa = attemptAnswers.FirstOrDefault(x => x.QuestionId == q.Id);

                    List<int> selectedIds = new();
                    if (!string.IsNullOrWhiteSpace(aa?.SelectedAnswerIdsJson))
                    {
                        try
                        {
                            selectedIds = JsonSerializer.Deserialize<List<int>>(aa.SelectedAnswerIdsJson!) ?? new();
                        }
                        catch
                        {
                            selectedIds = new();
                        }
                    }

                    return new QuizReviewQuestionDto
                    {
                        QuestionId = q.Id,
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType.ToString(),
                        Points = q.Points,
                        IsCorrect = aa?.IsCorrect ?? false,
                        SelectedAnswerId = aa?.AnswerId,
                        SelectedAnswerIds = selectedIds,
                        GivenTextAnswer = aa?.GivenTextAnswer,
                        CorrectAnswerIds = (q.Answers ?? new List<Answer>())
                            .Where(a => a.IsCorrect)
                            .Select(a => a.Id)
                            .ToList(),
                        Answers = (q.Answers ?? new List<Answer>())
                            .Select(a => new QuizReviewAnswerDto
                            {
                                AnswerId = a.Id,
                                AnswerText = a.AnswerText,
                                IsCorrect = a.IsCorrect
                            })
                            .ToList()
                    };
                }).ToList()
            };

            return review;
        }

        private static List<int> ParseSelectedQuestionIds(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<int>();

            try
            {
                return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
            }
            catch
            {
                return new List<int>();
            }
        }
    }
}
