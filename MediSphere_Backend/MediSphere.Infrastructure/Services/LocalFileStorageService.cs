using MediSphere.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace MediSphere.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;

    public LocalFileStorageService(IWebHostEnvironment env, string baseUrl)
    {
        _basePath = Path.Combine(env.WebRootPath ?? "wwwroot", "uploads");
        _baseUrl = baseUrl;
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string folder)
    {
        var directory = Path.Combine(_basePath, folder);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";

        var filePath = Path.Combine(directory, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await stream.CopyToAsync(fileStream);
        }

        return $"{_baseUrl}/uploads/{folder}/{uniqueFileName}";
    }

    public Task DeleteAsync(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return Task.CompletedTask;
        }

        var relativePath = fileUrl
            .Replace(_baseUrl, "")
            .TrimStart('/')
            .Replace('/', Path.DirectorySeparatorChar);

        var fullPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            relativePath.Replace("uploads\\", "")
        );

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}