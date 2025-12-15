using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using OLP.Core.Interfaces;
using OLP.Infrastructure.Services;

namespace OLP.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class CertificatesController : ControllerBase
    {
        private readonly CertificateService _certService;
        private readonly ICertificateRepository _certRepo;

        public CertificatesController(CertificateService certService, ICertificateRepository certRepo)
        {
            _certService = certService;
            _certRepo = certRepo;
        }

        [HttpPost("courses/{courseId}/certificate/generate")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GenerateForStudent(int courseId, [FromQuery] int userId)
        {
            var cert = await _certService.GenerateCertificateAsync(userId, courseId);
            return Ok(cert);
        }

        [HttpGet("certificates/{id}/download")]
        [Authorize]
        public async Task<IActionResult> Download(int id)
        {
            var cert = (await _certRepo.GetByUserAsync(0)).FirstOrDefault(c => c.Id == id); // you can add GetByIdAsync to repo
            if (cert == null) return NotFound();
            return Ok(new { cert.DownloadUrl });
        }

        [HttpGet("certificates/verify/{code}")]
        [AllowAnonymous]
        public async Task<IActionResult> Verify(string code)
        {
            var all = await _certRepo.GetByUserAsync(0); // again: you can add a repo method like GetByCodeAsync
            var cert = all.FirstOrDefault(c => c.VerificationCode == code);
            if (cert == null) return NotFound("Invalid or unknown certificate");
            return Ok(cert);
        }
    }
}


// POST /api/courses/{courseId}/certificate/generate → Admin/SuperAdmin (manual)

//GET / api / certificates /{ id}/ download → Student(or Admin)

//GET / api / certificates / verify /{ code} → Public(no auth)