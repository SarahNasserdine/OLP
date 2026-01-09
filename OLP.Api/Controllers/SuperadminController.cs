using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLP.Core.Interfaces;

namespace OLP.Api.Controllers
{
    [ApiController]
    [Route("api/superadmin")]
    public class SuperadminController : ControllerBase
    {
        private readonly IUserRepository _userRepo;
        private readonly ICourseRepository _courseRepo;

        public SuperadminController(IUserRepository userRepo, ICourseRepository courseRepo)
        {
            _userRepo = userRepo;
            _courseRepo = courseRepo;
        }

        [HttpGet("overview")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetOverview()
        {
            var users = await _userRepo.GetAllAsync();
            var courses = await _courseRepo.GetAllAsync();

            return Ok(new
            {
                totalUsers = users.Count(),
                totalCourses = courses.Count(),
                systemAlerts = 0
            });
        }
    }
}
