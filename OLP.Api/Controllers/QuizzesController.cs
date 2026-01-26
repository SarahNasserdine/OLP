using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using OLP.Core.DTOs;
using OLP.Core.Entities;
using OLP.Core.Interfaces;
using OLP.Core.Enums;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace OLP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuizzesController : ControllerBase
    {
        private readonly IQuizRepository _quizRepo;
        private readonly IQuestionRepository _questionRepo;
        private readonly IAnswerRepository _answerRepo;
        private readonly IQuizAttemptRepository _attemptRepo;
        private readonly IQuizService _quizService;

        public QuizzesController(
            IQuizRepository quizRepo,
            IQuestionRepository questionRepo,
            IAnswerRepository answerRepo,
            IQuizAttemptRepository attemptRepo,
            IQuizService quizService)
        {
            _quizRepo = quizRepo;
            _questionRepo = questionRepo;
            _answerRepo = answerRepo;
            _attemptRepo = attemptRepo;
            _quizService = quizService;
        }

        // ===============================
        // STUDENT ROUTES
        // ===============================

        // GET /api/quizzes/{id} - get quiz without correct answers
        // If ShuffleQuestions=true => randomize order each time (restart/refresh)
        
        // POST /api/quizzes/{id}/start - start a quiz attempt
        [HttpPost("{id}/start")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> StartQuiz(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var attempt = await _quizService.StartAttemptAsync(userId, id);

            return Ok(new
            {
                attempt.Id,
                attempt.QuizId,
                attempt.UserId,
                attempt.AttemptNumber,
                attempt.StartedAt
            });
        }

        // POST /api/courses/{courseId}/final-quiz/start - start final quiz with random 10 lesson questions
        [HttpPost("/api/courses/{courseId}/final-quiz/start")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> StartFinalQuiz(int courseId)
        {
            if (courseId <= 0)
                return BadRequest("CourseId must be a positive number.");

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var finalQuiz = await _quizRepo.GetFinalByCourseIdAsync(courseId);
            if (finalQuiz == null)
            {
                finalQuiz = new Quiz
                {
                    CourseId = courseId,
                    LessonId = null,
                    Title = "Final Quiz",
                    PassingScore = 70,
                    TimeLimit = null,
                    ShuffleQuestions = true,
                    AllowRetake = false,
                    IsFinal = true,
                    IsActive = true
                };

                await _quizRepo.AddAsync(finalQuiz);
                await _quizRepo.SaveChangesAsync();
            }

            if (!finalQuiz.IsActive)
                return BadRequest("Final quiz is not active.");

            var userAttempts = await _attemptRepo.GetByUserIdAsync(userId);
            var existingAttempt = userAttempts
                .FirstOrDefault(a => a.QuizId == finalQuiz.Id && !a.SubmittedAt.HasValue);

            if (existingAttempt != null)
            {
                var selectedIds = ParseSelectedQuestionIds(existingAttempt.SelectedQuestionIdsJson);
                if (selectedIds.Count > 0)
                {
                    var resumedQuestions = (await _questionRepo.GetByIdsWithAnswersAsync(selectedIds)).ToList();
                    if (resumedQuestions.Any())
                    {
                        var resumed = BuildFinalQuizResponse(finalQuiz, existingAttempt, resumedQuestions);
                        return Ok(resumed);
                    }
                }

                var retryQuestionPool = (await _questionRepo.GetLessonQuestionsByCourseIdAsync(courseId)).ToList();
                if (!retryQuestionPool.Any())
                    return BadRequest("Final quiz has no questions yet.");

                var retrySelectedQuestions = retryQuestionPool
                    .OrderBy(_ => Guid.NewGuid())
                    .Take(Math.Min(10, retryQuestionPool.Count))
                    .ToList();

                existingAttempt.SelectedQuestionIdsJson = JsonSerializer.Serialize(retrySelectedQuestions.Select(q => q.Id));
                await _attemptRepo.SaveChangesAsync();

                return Ok(BuildFinalQuizResponse(finalQuiz, existingAttempt, retrySelectedQuestions));
            }

            // Always allow retakes for final quiz.

            var questionPool = (await _questionRepo.GetLessonQuestionsByCourseIdAsync(courseId)).ToList();
            if (!questionPool.Any())
                return BadRequest("Final quiz has no questions yet.");

            var selectedQuestions = questionPool
                .OrderBy(_ => Guid.NewGuid())
                .Take(Math.Min(10, questionPool.Count))
                .ToList();

            var attempt = await _quizService.StartAttemptWithSelectedQuestionsAsync(
                userId,
                finalQuiz.Id,
                selectedQuestions.Select(q => q.Id));

            return Ok(BuildFinalQuizResponse(finalQuiz, attempt, selectedQuestions));
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetQuiz(int id)
        {
            var quiz = await _quizRepo.GetByIdWithQuestionsAsync(id);
            if (quiz == null)
                return NotFound();

            var questions = quiz.Questions?.AsEnumerable() ?? Enumerable.Empty<Question>();

            // Shuffle questions if enabled, otherwise stable order by OrderIndex
            questions = quiz.ShuffleQuestions
                ? questions.OrderBy(_ => Guid.NewGuid())
                : questions.OrderBy(q => q.OrderIndex).ThenBy(q => q.Id);

            var result = new
            {
                quiz.Id,
                quiz.CourseId,
                quiz.Title,
                quiz.PassingScore,
                quiz.TimeLimit,
                quiz.AllowRetake,
                quiz.ShuffleQuestions,
                Questions = questions.Select(q =>
                {
                    var answers = q.Answers?.AsEnumerable() ?? Enumerable.Empty<Answer>();

                    // Optional: shuffle answers too
                    answers = quiz.ShuffleQuestions
                        ? answers.OrderBy(_ => Guid.NewGuid())
                        : answers.OrderBy(a => a.OrderIndex ?? int.MaxValue).ThenBy(a => a.Id);

                    return new
                    {
                        q.Id,
                        q.QuestionText,
                        q.QuestionType,
                        q.Points,
                        Answers = answers.Select(a => new
                        {
                            a.Id,
                            a.AnswerText
                        })
                    };
                })
            };

            return Ok(result);
        }

        // POST /api/quizzes/{id}/submit - student submits quiz
        [HttpPost("{id}/submit")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> SubmitQuiz(int id, [FromBody] SubmitQuizDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            dto.QuizId = id;

            var attempt = await _quizService.SubmitAttemptAsync(userId, dto);

            // Return a safe response (avoid returning entities with navigation cycles)
            return Ok(new
            {
                attempt.Id,
                attempt.QuizId,
                attempt.UserId,
                attempt.Score,
                attempt.AttemptNumber,
                attempt.AttemptDate
            });
        }

        // GET /api/quizzes/{id}/attempts - get all attempts by current user for this quiz
        [HttpGet("{id}/attempts")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetUserAttempts(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var attempts = await _attemptRepo.GetByUserIdAsync(userId);

            var filtered = attempts
                .Where(a => a.QuizId == id)
                .Select(a => new
                {
                    a.Id,
                    a.QuizId,
                    a.Score,
                    a.AttemptNumber,
                    a.AttemptDate
                });

            return Ok(filtered);
        }

        // GET /api/quizzes/attempts/{attemptId}/review - review attempt details
        [HttpGet("attempts/{attemptId}/review")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> ReviewAttempt(int attemptId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var review = await _quizService.GetAttemptReviewAsync(attemptId, userId);
            return Ok(review);
        }

        // ===============================
        // ADMIN / SUPERADMIN ROUTES
        // ===============================

        // GET /api/admin/quiz-attempts?userId=&quizId=&courseId=
        [HttpGet("/api/admin/quiz-attempts")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetAdminAttempts([FromQuery] int? userId, [FromQuery] int? quizId, [FromQuery] int? courseId)
        {
            var attempts = await _attemptRepo.GetAdminAttemptsAsync(userId, quizId, courseId);

            var result = attempts.Select(a => new
            {
                a.Id,
                a.QuizId,
                QuizTitle = a.Quiz?.Title ?? "",
                CourseId = a.Quiz?.CourseId,
                CourseTitle = a.Quiz?.Course?.Title ?? "",
                a.UserId,
                UserName = a.User?.FullName ?? "",
                UserEmail = a.User?.Email ?? "",
                a.Score,
                a.AttemptNumber,
                a.AttemptDate,
                a.SubmittedAt
            });

            return Ok(result);
        }

        // GET /api/admin/quiz-attempts/{attemptId}/review
        [HttpGet("/api/admin/quiz-attempts/{attemptId}/review")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> ReviewAttemptForAdmin(int attemptId)
        {
            try
            {
                var review = await _quizService.GetAttemptReviewForAdminAsync(attemptId);
                return Ok(review);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET /api/courses/{courseId}/quizzes (Student/Admin/SuperAdmin)
        [HttpGet("/api/courses/{courseId}/quizzes")]
        [Authorize]
        public async Task<IActionResult> GetByCourse(int courseId)
        {
            var quizzes = await _quizRepo.GetByCourseIdAsync(courseId);

            var result = quizzes.Select(q => new
            {
                q.Id,
                q.CourseId,
                q.LessonId,
                q.Title,
                q.PassingScore,
                q.TimeLimit,
                q.AllowRetake,
                q.ShuffleQuestions,
                q.IsActive,
                q.IsFinal,
                QuestionsCount = q.Questions?.Count ?? 0
            });

            return Ok(result);
        }

        // GET /api/quizzes (Admin/SuperAdmin) - list quizzes, optionally filter by courseId
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetQuizzes([FromQuery] int? courseId)
        {
            IEnumerable<Quiz> quizzes;

            if (courseId.HasValue)
            {
                quizzes = await _quizRepo.GetByCourseIdAsync(courseId.Value);
            }
            else
            {
                quizzes = await _quizRepo.GetAllWithCourseAsync();
            }

            var result = quizzes.Select(q => new
            {
                q.Id,
                q.CourseId,
                q.LessonId,
                CourseTitle = q.Course?.Title,
                q.Title,
                q.PassingScore,
                q.TimeLimit,
                q.AllowRetake,
                q.ShuffleQuestions,
                q.IsActive,
                q.IsFinal,
                QuestionsCount = q.Questions?.Count ?? 0
            });

            return Ok(result);
        }

        // POST /api/quizzes - create a new quiz
        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> CreateQuiz([FromBody] QuizCreateDto dto)
        {
            if (dto.CourseId <= 0)
                return BadRequest("CourseId must be a positive number.");

            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest("Title is required.");

            if (dto.PassingScore < 0)
                return BadRequest("PassingScore must be >= 0.");

            if (dto.TimeLimit < 0)
                return BadRequest("TimeLimit must be >= 0.");

            var quiz = new Quiz
            {
                CourseId = dto.CourseId,
                LessonId = dto.LessonId,
                Title = dto.Title.Trim(),
                PassingScore = dto.PassingScore,
                TimeLimit = dto.TimeLimit == 0 ? (int?)null : dto.TimeLimit, // 0 => no limit
                ShuffleQuestions = dto.ShuffleQuestions,
                AllowRetake = dto.AllowRetake
            };

            await _quizRepo.AddAsync(quiz);
            await _quizRepo.SaveChangesAsync();

            // SAFE response (avoid cycles)
            return Ok(new
            {
                quiz.Id,
                quiz.CourseId,
                quiz.LessonId,
                quiz.Title,
                quiz.PassingScore,
                quiz.TimeLimit,
                quiz.ShuffleQuestions,
                quiz.AllowRetake,
                quiz.IsActive
            });
        }

        // POST /api/quizzes/{quizId}/questions - add a question (with answers)
        [HttpPost("{quizId}/questions")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> AddQuestion(int quizId, [FromBody] QuestionCreateDto dto)
        {
            var quiz = await _quizRepo.GetByIdAsync(quizId);
            if (quiz == null)
                return NotFound("Quiz not found.");

            if (string.IsNullOrWhiteSpace(dto.QuestionText))
                return BadRequest("QuestionText is required.");

            if (!Enum.TryParse<QuestionType>(dto.QuestionType, true, out var parsedType))
                return BadRequest("Invalid QuestionType. Allowed: MCQ, MSQ, TF, ShortAnswer");

            if (dto.Points <= 0)
                return BadRequest("Points must be >= 1.");

            var answers = dto.Answers ?? new List<AnswerCreateDto>();
            var correctCount = answers.Count(a => a.IsCorrect);

            // Validate answers by type
            if (parsedType == QuestionType.ShortAnswer)
            {
                answers.Clear();
            }
            else if (parsedType == QuestionType.TF)
            {
                if (answers.Count != 2) return BadRequest("TF must have exactly 2 answers: True and False.");
                if (correctCount != 1) return BadRequest("TF must have exactly 1 correct answer.");
            }
            else if (parsedType == QuestionType.MCQ)
            {
                if (answers.Count < 2) return BadRequest("MCQ must have at least 2 answers.");
                if (correctCount != 1) return BadRequest("MCQ must have exactly 1 correct answer.");
            }
            else if (parsedType == QuestionType.MSQ)
            {
                if (answers.Count < 2) return BadRequest("MSQ must have at least 2 answers.");
                if (correctCount < 1) return BadRequest("MSQ must have at least 1 correct answer.");
            }

            // ✅ Create Question (includes OrderIndex)
            var question = new Question
            {
                QuizId = quizId,
                QuestionText = dto.QuestionText.Trim(),
                QuestionType = parsedType,
                Points = dto.Points,
                OrderIndex = dto.OrderIndex
            };

            await _questionRepo.AddAsync(question);
            await _questionRepo.SaveChangesAsync();

            // ✅ Create Answers (includes nullable OrderIndex)
            foreach (var ans in answers)
            {
                if (string.IsNullOrWhiteSpace(ans.AnswerText))
                    return BadRequest("AnswerText is required for each answer.");

                var answer = new Answer
                {
                    QuestionId = question.Id,
                    AnswerText = ans.AnswerText.Trim(),
                    IsCorrect = ans.IsCorrect,
                    OrderIndex = ans.OrderIndex
                };

                await _answerRepo.AddAsync(answer);
            }

            await _answerRepo.SaveChangesAsync();

            // ✅ SAFE response (NO cycles)
            return Ok(new
            {
                question.Id,
                question.QuizId,
                question.QuestionText,
                QuestionType = question.QuestionType.ToString(),
                question.Points,
                question.OrderIndex
            });
        }

        // PUT /api/courses/{courseId}/final-quiz - update final quiz settings
        [HttpPut("/api/courses/{courseId}/final-quiz")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateFinalQuizSettings(
            int courseId,
            [FromBody] FinalQuizSettingsDto dto)
        {
            if (courseId <= 0)
                return BadRequest("CourseId must be a positive number.");

            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest("Title is required.");

            if (dto.PassingScore < 0)
                return BadRequest("PassingScore must be >= 0.");

            if (dto.TimeLimit < 0)
                return BadRequest("TimeLimit must be >= 0.");

            var finalQuiz = await _quizRepo.GetFinalByCourseIdAsync(courseId);
            if (finalQuiz == null)
            {
                finalQuiz = new Quiz
                {
                    CourseId = courseId,
                    LessonId = null,
                    IsFinal = true
                };

                await _quizRepo.AddAsync(finalQuiz);
            }

            finalQuiz.Title = dto.Title.Trim();
            finalQuiz.PassingScore = dto.PassingScore;
            finalQuiz.TimeLimit = dto.TimeLimit == 0 ? (int?)null : dto.TimeLimit;
            finalQuiz.ShuffleQuestions = dto.ShuffleQuestions;
            finalQuiz.AllowRetake = dto.AllowRetake;
            finalQuiz.IsActive = dto.IsActive;
            finalQuiz.IsFinal = true;
            finalQuiz.LessonId = null;

            await _quizRepo.SaveChangesAsync();

            return Ok(new
            {
                finalQuiz.Id,
                finalQuiz.CourseId,
                finalQuiz.Title,
                finalQuiz.PassingScore,
                finalQuiz.TimeLimit,
                finalQuiz.ShuffleQuestions,
                finalQuiz.AllowRetake,
                finalQuiz.IsActive,
                finalQuiz.IsFinal
            });
        }

        // PUT /api/quizzes/{id} - update quiz details (Admin/SuperAdmin)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateQuiz(int id, [FromBody] QuizCreateDto dto)
        {
            if (dto.CourseId <= 0)
                return BadRequest("CourseId must be a positive number.");

            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest("Title is required.");

            if (dto.PassingScore < 0)
                return BadRequest("PassingScore must be >= 0.");

            if (dto.TimeLimit < 0)
                return BadRequest("TimeLimit must be >= 0.");

            var quiz = await _quizRepo.GetByIdAsync(id);
            if (quiz == null)
                return NotFound("Quiz not found.");

            quiz.CourseId = dto.CourseId;
            quiz.LessonId = dto.LessonId;
            quiz.Title = dto.Title.Trim();
            quiz.PassingScore = dto.PassingScore;
            quiz.TimeLimit = dto.TimeLimit == 0 ? (int?)null : dto.TimeLimit;
            quiz.ShuffleQuestions = dto.ShuffleQuestions;
            quiz.AllowRetake = dto.AllowRetake;

            await _quizRepo.SaveChangesAsync();

            return Ok(new
            {
                quiz.Id,
                quiz.CourseId,
                quiz.LessonId,
                quiz.Title,
                quiz.PassingScore,
                quiz.TimeLimit,
                quiz.ShuffleQuestions,
                quiz.AllowRetake,
                quiz.IsActive
            });
        }



        // DELETE /api/questions/{id} - delete question
        [HttpDelete("/api/questions/{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = await _questionRepo.GetByIdAsync(id);
            if (question == null)
                return NotFound();

            await _questionRepo.DeleteAsync(question);
            await _questionRepo.SaveChangesAsync();
            return Ok();
        }

        // DELETE /api/answers/{id} - delete answer
        [HttpDelete("/api/answers/{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteAnswer(int id)
        {
            var answer = await _answerRepo.GetByIdAsync(id);
            if (answer == null)
                return NotFound();

            await _answerRepo.DeleteAsync(answer);
            await _answerRepo.SaveChangesAsync();
            return Ok();
        }

        private static object BuildFinalQuizResponse(Quiz finalQuiz, QuizAttempt attempt, IEnumerable<Question> questions)
        {
            return new
            {
                finalQuiz.Id,
                finalQuiz.CourseId,
                finalQuiz.Title,
                finalQuiz.PassingScore,
                finalQuiz.TimeLimit,
                finalQuiz.AllowRetake,
                finalQuiz.ShuffleQuestions,
                AttemptId = attempt.Id,
                attempt.AttemptNumber,
                attempt.StartedAt,
                Questions = questions.Select(q =>
                {
                    var answers = q.Answers?.AsEnumerable() ?? Enumerable.Empty<Answer>();

                    answers = finalQuiz.ShuffleQuestions
                        ? answers.OrderBy(_ => Guid.NewGuid())
                        : answers.OrderBy(a => a.OrderIndex ?? int.MaxValue).ThenBy(a => a.Id);

                    return new
                    {
                        q.Id,
                        q.QuestionText,
                        q.QuestionType,
                        q.Points,
                        Answers = answers.Select(a => new
                        {
                            a.Id,
                            a.AnswerText
                        })
                    };
                })
            };
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
