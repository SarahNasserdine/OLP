using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLP.Api.Services;

namespace OLP.Api.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FilesController : ControllerBase
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ICloudinaryService _cloudinaryService;

        public FilesController(IHttpClientFactory clientFactory, ICloudinaryService cloudinaryService)
        {
            _clientFactory = clientFactory;
            _cloudinaryService = cloudinaryService;
        }

        // GET /api/files/pdf?url={cloudinaryUrl}
        [HttpGet("pdf")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPdf([FromQuery] string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return BadRequest("Missing url.");

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return BadRequest("Invalid url.");

            if (!uri.Host.Equals("res.cloudinary.com", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Unsupported host.");

            if (!uri.AbsolutePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only PDF files are supported.");

            var client = _clientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0");
            request.Headers.Accept.ParseAdd("application/pdf");

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, body);
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            return File(bytes, "application/pdf");
        }

        // GET /api/files/signed-pdf?publicId=...&deliveryType=private&url=...
        [HttpGet("signed-pdf")]
        [Authorize]
        public async Task<IActionResult> GetSignedPdf([FromQuery] string publicId, [FromQuery] string? deliveryType, [FromQuery] string? url)
        {
            if (string.IsNullOrWhiteSpace(publicId) && string.IsNullOrWhiteSpace(url))
                return BadRequest("Missing publicId or url.");

            var resolvedPublicId = publicId;
            var resolvedDeliveryType = deliveryType;
            int? version = null;

            if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                var path = uri.AbsolutePath;
                var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

                // expected: /<cloud>/raw/<delivery>/v1234/<publicId>
                var rawIndex = Array.FindIndex(parts, p => p.Equals("raw", StringComparison.OrdinalIgnoreCase));
                if (rawIndex >= 0 && parts.Length > rawIndex + 2)
                {
                    resolvedDeliveryType ??= parts[rawIndex + 1];

                    var versionPart = parts[rawIndex + 2];
                    if (versionPart.StartsWith("v") && int.TryParse(versionPart[1..], out var parsed))
                    {
                        version = parsed;
                        resolvedPublicId = string.Join('/', parts.Skip(rawIndex + 3));
                    }
                    else
                    {
                        resolvedPublicId = string.Join('/', parts.Skip(rawIndex + 2));
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(resolvedPublicId))
                return BadRequest("Missing publicId.");

            if (!version.HasValue)
            {
                var resource = await _cloudinaryService.TryGetRawResourceAsync(resolvedPublicId);
                if (resource.HasValue)
                {
                    resolvedDeliveryType = resource.Value.DeliveryType;
                    version = resource.Value.Version;
                    resolvedPublicId = resource.Value.PublicId;
                }
            }

            if (string.IsNullOrWhiteSpace(resolvedDeliveryType) || resolvedDeliveryType.Equals("upload", StringComparison.OrdinalIgnoreCase))
            {
                var probedDeliveryType = await ProbeDeliveryTypeAsync(resolvedPublicId, version);
                if (!string.IsNullOrWhiteSpace(probedDeliveryType))
                {
                    resolvedDeliveryType = probedDeliveryType;
                }
            }

            try
            {
                var signedUrl = _cloudinaryService.GetSignedRawUrl(
                    resolvedPublicId,
                    resolvedDeliveryType ?? "private",
                    "pdf",
                    version);

                return Ok(new { url = signedUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task<string?> ProbeDeliveryTypeAsync(string publicId, int? version)
        {
            var candidates = new[] { "authenticated", "private", "upload" };
            foreach (var candidate in candidates)
            {
                try
                {
                    var candidateUrl = _cloudinaryService.GetSignedRawUrl(publicId, candidate, "pdf", version);
                    var client = _clientFactory.CreateClient();
                    using var request = new HttpRequestMessage(HttpMethod.Head, candidateUrl);
                    using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    if (response.IsSuccessStatusCode)
                        return candidate;
                }
                catch
                {
                    // Ignore failures and try the next delivery type.
                }
            }

            return null;
        }
    }
}
