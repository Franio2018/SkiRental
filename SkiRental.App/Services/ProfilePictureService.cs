using Microsoft.Extensions.Logging;

namespace SkiRental.App.Services;

/// <summary>
/// Service for handling profile picture uploads and storage
/// </summary>
public class ProfilePictureService
{
    private readonly ILogger<ProfilePictureService> _logger;
    private readonly string _profilePicturesPath;

    public ProfilePictureService(ILogger<ProfilePictureService> logger)
    {
        _logger = logger;
        _profilePicturesPath = Path.Combine(FileSystem.AppDataDirectory, "ProfilePictures");
        
        // Ensure directory exists
        if (!Directory.Exists(_profilePicturesPath))
        {
            Directory.CreateDirectory(_profilePicturesPath);
        }
    }

    /// <summary>
    /// Uploads and saves a profile picture
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="fileStream">File stream of the image</param>
    /// <param name="fileName">Original filename</param>
    /// <returns>Relative path to the saved image</returns>
    public async Task<string> UploadProfilePictureAsync(int userId, Stream fileStream, string fileName)
    {
        try
        {
            // Validate file type
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
            
            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException("Nieobsugiwany format pliku. Dozwolone formaty: JPG, PNG, GIF, BMP");
            }

            // Generate unique filename
            var uniqueFileName = $"user_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
            var filePath = Path.Combine(_profilePicturesPath, uniqueFileName);

            // Save file
            using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileStreamOutput);
            }

            _logger.LogInformation("Profile picture uploaded successfully for user {UserId}: {FileName}", userId, uniqueFileName);
            
            return uniqueFileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile picture for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Gets the full path to a profile picture
    /// </summary>
    /// <param name="fileName">Filename of the profile picture</param>
    /// <returns>Full path to the image file</returns>
    public string GetProfilePicturePath(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return string.Empty;

        var fullPath = Path.Combine(_profilePicturesPath, fileName);
        return File.Exists(fullPath) ? fullPath : string.Empty;
    }

    /// <summary>
    /// Deletes a profile picture
    /// </summary>
    /// <param name="fileName">Filename of the profile picture to delete</param>
    public async Task DeleteProfilePictureAsync(string fileName)
    {
        try
        {
            if (string.IsNullOrEmpty(fileName))
                return;

            var fullPath = Path.Combine(_profilePicturesPath, fileName);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Profile picture deleted: {FileName}", fileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile picture: {FileName}", fileName);
            throw;
        }
    }

    /// <summary>
    /// Gets a stream for serving the profile picture
    /// </summary>
    /// <param name="fileName">Filename of the profile picture</param>
    /// <returns>Stream of the image file or null if not found</returns>
    public Stream? GetProfilePictureStream(string fileName)
    {
        try
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            var fullPath = Path.Combine(_profilePicturesPath, fileName);
            if (!File.Exists(fullPath))
                return null;

            return new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile picture stream: {FileName}", fileName);
            return null;
        }
    }
}
