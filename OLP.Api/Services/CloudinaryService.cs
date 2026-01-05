using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace OLP.Api.Services
{
	public class CloudinarySettings
	{
		public string CloudName { get; set; } = "";
		public string ApiKey { get; set; } = "";
		public string ApiSecret { get; set; } = "";
	}

	public interface ICloudinaryService
	{
		Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file, string folder);
		Task<(string Url, string PublicId)> UploadFileAsync(IFormFile file, string folder); // pdf, etc.
		Task<(string Url, string PublicId)> UploadVideoAsync(IFormFile file, string folder);
	}

	public class CloudinaryService : ICloudinaryService
	{
		private readonly Cloudinary _cloudinary;

		public CloudinaryService(IOptions<CloudinarySettings> config)
		{
			var s = config.Value;
			_cloudinary = new Cloudinary(new Account(s.CloudName, s.ApiKey, s.ApiSecret));
		}

		public async Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file, string folder)
		{
			using var stream = file.OpenReadStream();
			var uploadParams = new ImageUploadParams
			{
				File = new FileDescription(file.FileName, stream),
				Folder = folder
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
				Folder = folder
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
				Folder = folder
			};

			var result = await _cloudinary.UploadAsync(uploadParams);
			if (result.Error != null) throw new Exception(result.Error.Message);

			return (result.SecureUrl.ToString(), result.PublicId);
		}
	}
}
