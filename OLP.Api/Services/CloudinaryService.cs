//using CloudinaryDotNet;
//using CloudinaryDotNet.Actions;
//using Microsoft.Extensions.Options;

//namespace OLP.Api.Services
//{
//	public class CloudinarySettings
//	{
//		public string CloudName { get; set; } = "";
//		public string ApiKey { get; set; } = "";
//		public string ApiSecret { get; set; } = "";
//	}

//	public interface ICloudinaryService
//	{
//		Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file, string folder);
//		Task<(string Url, string PublicId)> UploadFileAsync(IFormFile file, string folder); // pdf, etc.
//		Task<(string Url, string PublicId)> UploadVideoAsync(IFormFile file, string folder);
//	}

//	public class CloudinaryService : ICloudinaryService
//	{
//		private readonly Cloudinary _cloudinary;

//		public CloudinaryService(IOptions<CloudinarySettings> config)
//		{
//			var s = config.Value;
//			_cloudinary = new Cloudinary(new Account(s.CloudName, s.ApiKey, s.ApiSecret));
//		}

//		public async Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file, string folder)
//		{
//			using var stream = file.OpenReadStream();
//			var uploadParams = new ImageUploadParams
//			{
//				File = new FileDescription(file.FileName, stream),
//				Folder = folder
//			};

//			var result = await _cloudinary.UploadAsync(uploadParams);
//			if (result.Error != null) throw new Exception(result.Error.Message);

//			return (result.SecureUrl.ToString(), result.PublicId);
//		}

//		public async Task<(string Url, string PublicId)> UploadVideoAsync(IFormFile file, string folder)
//		{
//			using var stream = file.OpenReadStream();
//			var uploadParams = new VideoUploadParams
//			{
//				File = new FileDescription(file.FileName, stream),
//				Folder = folder
//			};

//			var result = await _cloudinary.UploadAsync(uploadParams);
//			if (result.Error != null) throw new Exception(result.Error.Message);

//			return (result.SecureUrl.ToString(), result.PublicId);
//		}

//		public async Task<(string Url, string PublicId)> UploadFileAsync(IFormFile file, string folder)
//		{
//			using var stream = file.OpenReadStream();
//			var uploadParams = new RawUploadParams
//			{
//				File = new FileDescription(file.FileName, stream),
//				Folder = folder
//			};

//			var result = await _cloudinary.UploadAsync(uploadParams);
//			if (result.Error != null) throw new Exception(result.Error.Message);

//			return (result.SecureUrl.ToString(), result.PublicId);
//		}
//	}
//}


using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace OLP.Api.Services
{
    public class CloudinarySettings
    {
        public string CloudName { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public string ApiSecret { get; set; } = "";
        public string AuthTokenKey { get; set; } = "";
    }

    public interface ICloudinaryService
    {
        Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file, string folder);
        Task<(string Url, string PublicId)> UploadFileAsync(IFormFile file, string folder);  // pdf, etc.
        Task<(string Url, string PublicId)> UploadVideoAsync(IFormFile file, string folder);

        // ✅ NEW: delete from cloudinary by publicId + type
        // resourceType: "image" | "video" | "raw"
        Task DeleteAsync(string publicId, string resourceType);
        string GetSignedRawUrl(string publicId, string deliveryType = "private", string? format = null, int? version = null);
        Task<(string DeliveryType, int Version, string PublicId)?> TryGetRawResourceAsync(string publicId);
    }

    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly string? _authTokenKey;

        public CloudinaryService(IOptions<CloudinarySettings> config)
        {
            var s = config.Value;

            // ✅ basic validation (helps catch wrong appsettings values early)
            if (string.IsNullOrWhiteSpace(s.CloudName) ||
                string.IsNullOrWhiteSpace(s.ApiKey) ||
                string.IsNullOrWhiteSpace(s.ApiSecret))
            {
                throw new Exception("Cloudinary settings are missing. Check appsettings.json (Cloudinary:CloudName/ApiKey/ApiSecret).");
            }

            _authTokenKey = IsValidHex(s.AuthTokenKey) ? s.AuthTokenKey : null;
            _cloudinary = new Cloudinary(new Account(s.CloudName, s.ApiKey, s.ApiSecret));
            _cloudinary.Api.Secure = true;
        }

        public async Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file, string folder)
        {
            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.Error != null) throw new Exception(result.Error.Message);

            return (result.SecureUrl.ToString(), result.PublicId);
        }

        public async Task<(string Url, string PublicId)> UploadVideoAsync(IFormFile file, string folder)
        {
            using var stream = file.OpenReadStream();

            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.Error != null) throw new Exception(result.Error.Message);

            return (result.SecureUrl.ToString(), result.PublicId);
        }

        public async Task<(string Url, string PublicId)> UploadFileAsync(IFormFile file, string folder)
        {
            using var stream = file.OpenReadStream();

            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false,
                AccessMode = "public"
            };

            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.Error != null) throw new Exception(result.Error.Message);

            return (result.SecureUrl.ToString(), result.PublicId);
        }

        public async Task DeleteAsync(string publicId, string resourceType)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                return;

            var rt = resourceType?.Trim().ToLowerInvariant();

            // Cloudinary resource types:
            // image -> ResourceType.Image
            // video -> ResourceType.Video
            // raw   -> ResourceType.Raw (PDF, zip, etc.)
            var deletionParams = new DeletionParams(publicId)
            {
                ResourceType = rt switch
                {
                    "video" => ResourceType.Video,
                    "raw" => ResourceType.Raw,
                    _ => ResourceType.Image
                }
            };

            var result = await _cloudinary.DestroyAsync(deletionParams);

            // If not found, Cloudinary can return "not found" result; we won't throw for that
            if (result.Error != null)
                throw new Exception(result.Error.Message);
        }

        public string GetSignedRawUrl(string publicId, string deliveryType = "private", string? format = null, int? version = null)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                throw new Exception("Missing publicId.");
            var normalizedId = publicId;
            var detectedFormat = format;

            if (publicId.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                normalizedId = publicId[..^4];
                detectedFormat = "pdf";
            }

            var url = _cloudinary.Api.UrlImgUp
                .ResourceType("raw")
                .Type(deliveryType)
                .Secure(true)
                .Signed(true);

            if (deliveryType.Equals("authenticated", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(_authTokenKey))
                    throw new Exception("Cloudinary AuthTokenKey is missing or invalid. Add Cloudinary:AuthTokenKey (hex) in appsettings.");

                url = url.AuthToken(new AuthToken(_authTokenKey)
                    .Duration(3600)
                    .Acl(new[] { "/raw/authenticated/*" }));
            }

            if (!string.IsNullOrWhiteSpace(detectedFormat))
            {
                url = url.Format(detectedFormat);
            }

            if (version.HasValue)
            {
                url = url.Version(version.Value.ToString());
            }

            return url.BuildUrl(normalizedId);
        }

        public async Task<(string DeliveryType, int Version, string PublicId)?> TryGetRawResourceAsync(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                return null;

            var normalizedId = publicId.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                ? publicId[..^4]
                : publicId;

            var deliveryTypes = new[] { "authenticated", "private", "upload" };

            foreach (var deliveryType in deliveryTypes)
            {
                try
                {
                    var result = await _cloudinary.GetResourceAsync(new GetResourceParams(normalizedId)
                    {
                        ResourceType = ResourceType.Raw,
                        Type = deliveryType
                    });

                    if (result?.Error == null && !string.IsNullOrWhiteSpace(result.PublicId))
                    {
                        var accessMode = result.AccessMode?.Trim().ToLowerInvariant();
                        var resolvedType = accessMode switch
                        {
                            "authenticated" => "authenticated",
                            "private" => "private",
                            _ => string.IsNullOrWhiteSpace(result.Type) ? deliveryType : result.Type
                        };
                        var resolvedVersion = Convert.ToInt32(result.Version);
                        return (resolvedType, resolvedVersion, result.PublicId);
                    }
                }
                catch
                {
                    // Try the next delivery type when a resource lookup fails.
                }
            }

            return null;
        }

        private static bool IsValidHex(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length % 2 != 0)
                return false;

            foreach (var ch in value)
            {
                var isHex = (ch >= '0' && ch <= '9')
                         || (ch >= 'a' && ch <= 'f')
                         || (ch >= 'A' && ch <= 'F');
                if (!isHex)
                    return false;
            }

            return true;
        }
    }
}
