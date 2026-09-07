using MediSphere.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace MediSphere.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _webRoot;
    private readonly string _basePath;
    private readonly string _baseUrl;

    public LocalFileStorageService(IWebHostEnvironment env, string baseUrl)
    {
        _webRoot = !string.IsNullOrEmpty(env.WebRootPath)
            ? env.WebRootPath
            : Path.Combine(env.ContentRootPath, "wwwroot");

        _basePath = Path.Combine(_webRoot, "uploads");
        _baseUrl = baseUrl.TrimEnd('/');

        if (!Directory.Exists(_webRoot))
        {
            Directory.CreateDirectory(_webRoot);
        }

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string folder)
    {
        var sanitizedFolder = Path.GetFileName(folder); // Prevent folder traversal
        var directory = Path.Combine(_basePath, sanitizedFolder);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var cleanFileName = Path.GetFileName(fileName);
        var uniqueFileName = $"{Guid.NewGuid():N}_{cleanFileName}";
        var filePath = Path.Combine(directory, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await stream.CopyToAsync(fileStream);
        }

        return string.IsNullOrEmpty(_baseUrl)
            ? $"/uploads/{sanitizedFolder}/{uniqueFileName}"
            : $"{_baseUrl}/uploads/{sanitizedFolder}/{uniqueFileName}";
    }

    public Task<(Stream Stream, string ContentType)?> GetFileAsync(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return Task.FromResult<(Stream Stream, string ContentType)?>(null);
        }

        var relativePath = fileUrl;
        if (!string.IsNullOrEmpty(_baseUrl) && relativePath.StartsWith(_baseUrl, StringComparison.OrdinalIgnoreCase))
        {
            relativePath = relativePath.Substring(_baseUrl.Length);
        }

        relativePath = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

        // relativePath is e.g. "uploads/medical-records/uniqueFileName"
        var fullPath = Path.GetFullPath(Path.Combine(_webRoot, relativePath));

        // Path traversal defense: ensure fullPath is within _webRoot
        if (!fullPath.StartsWith(_webRoot, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<(Stream Stream, string ContentType)?>(null);
        }

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<(Stream Stream, string ContentType)?>(null);
        }

        var contentType = GetContentType(fullPath);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<(Stream Stream, string ContentType)?>((stream, contentType));
    }

    public Task DeleteAsync(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return Task.CompletedTask;
        }

        var relativePath = fileUrl;
        if (!string.IsNullOrEmpty(_baseUrl) && relativePath.StartsWith(_baseUrl, StringComparison.OrdinalIgnoreCase))
        {
            relativePath = relativePath.Substring(_baseUrl.Length);
        }

        relativePath = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

        var fullPath = Path.GetFullPath(Path.Combine(_webRoot, relativePath));

        if (fullPath.StartsWith(_webRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
        {
            try
            {
                File.Delete(fullPath);
            }
            catch
            {
                // Ignore delete errors
            }
        }

        return Task.CompletedTask;
    }

    private static string GetContentType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}