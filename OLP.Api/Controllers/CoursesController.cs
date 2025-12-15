using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using OLP.Core.DTOs;
using OLP.Core.Entities;
using OLP.Core.Enums;
using OLP.Core.Interfaces;
using OLP.Infrastructure.Services;

namespace OLP.Api.Controllers
{
    [ApiController]
    [Route("api/courses")]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseRepository _courseRepo;
        private readonly CourseService _courseService;

        public CoursesController(ICourseRepository courseRepo, CourseService courseService)
        {
            _courseRepo = courseRepo;
            _courseService = courseService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var courses = await _courseRepo.GetAllAsync();
            return Ok(courses);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();
            return Ok(course);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Create([FromBody] CourseCreateDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var course = new Course
            {
                Title = dto.Title,
                ShortDescription = dto.ShortDescription,
                LongDescription = dto.LongDescription,
                Category = dto.Category,
                Difficulty = Enum.Parse<DifficultyLevel>(dto.Difficulty),
                EstimatedDuration = dto.EstimatedDuration,
                Thumbnail = dto.Thumbnail,
                CreatedById = userId
            };

            await _courseService.CreateCourseAsync(course);
            return Ok(course);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Update(int id, [FromBody] CourseCreateDto dto)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();

            course.Title = dto.Title;
            course.ShortDescription = dto.ShortDescription;
            course.LongDescription = dto.LongDescription;
            course.Category = dto.Category;
            course.Difficulty = Enum.Parse<DifficultyLevel>(dto.Difficulty);
            course.EstimatedDuration = dto.EstimatedDuration;
            course.Thumbnail = dto.Thumbnail;

            await _courseRepo.SaveChangesAsync();
            return Ok(course);
        }

        [HttpPost("{id}/publish")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Publish(int id)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();

            course.IsPublished = true;
            await _courseRepo.SaveChangesAsync();

            return Ok(new { message = "Course published" });
        }
    }
}


// GET /api/courses → Student/Admin/SuperAdmin

//GET / api / courses /{ id} → Student / Admin / SuperAdmin

//POST / api / courses → Admin / SuperAdmin(create)

//PUT / api / courses /{ id} → Admin / SuperAdmin

//POST / api / courses /{ id}/ publish → Admin / SuperAdmin