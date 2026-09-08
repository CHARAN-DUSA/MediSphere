using System.Net.Http.Headers;
using MediSphere.Application.Interfaces;

namespace MediSphere.Infrastructure.Services;

/// <summary>
/// Stores files in Supabase Storage instead of local disk, using the service-role key
/// so it works against private buckets without needing storage RLS policies.
/// The "folder" parameter is treated as the bucket name (e.g. "medical-records", "doctor-images").
/// </summary>
public class SupabaseFileStorageService : IFileStorageService
{
    private readonly HttpClient _httpClient;
    private readonly string _projectUrl;
    private readonly string _serviceRoleKey;

    public SupabaseFileStorageService(HttpClient httpClient, string projectUrl, string serviceRoleKey)
    {
        _httpClient = httpClient;
        _projectUrl = projectUrl.TrimEnd('/');
        _serviceRoleKey = serviceRoleKey;
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string folder)
    {
        var bucket = folder;
        var cleanFileName = Path.GetFileName(fileName);
        var objectPath = $"{Guid.NewGuid():N}_{cleanFileName}";

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        using var content = new ByteArrayContent(ms.ToArray());
        content.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(cleanFileName));

        var request = new HttpRequestMessage(
            HttpMethod.Post, $"{_projectUrl}/storage/v1/object/{bucket}/{objectPath}")
        {
            Content = content
        };
        AddAuthHeaders(request);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Supabase upload failed ({response.StatusCode}): {error}");
        }

        // This is what gets persisted in the DB (MedicalRecord.FileUrl, Doctor.ProfileImageStorageKey).
        return $"{bucket}/{objectPath}";
    }

    public async Task<(Stream Stream, string ContentType)?> GetFileAsync(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return null;

        var (bucket, objectPath) = ParseFileUrl(fileUrl);
        if (bucket == null) return null;

        var request = new HttpRequestMessage(
            HttpMethod.Get, $"{_projectUrl}/storage/v1/object/{bucket}/{objectPath}");
        AddAuthHeaders(request);

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var contentType = response.Content.Headers.ContentType?.MediaType ?? GetContentType(objectPath);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        return (new MemoryStream(bytes), contentType);
    }

    public async Task DeleteAsync(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return;

        var (bucket, objectPath) = ParseFileUrl(fileUrl);
        if (bucket == null) return;

        var request = new HttpRequestMessage(
            HttpMethod.Delete, $"{_projectUrl}/storage/v1/object/{bucket}/{objectPath}");
        AddAuthHeaders(request);

        try { await _httpClient.SendAsync(request); }
        catch { /* best-effort delete, matches prior LocalFileStorageService behaviour */ }
    }

    private void AddAuthHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("apikey", _serviceRoleKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
    }

    private static (string? Bucket, string ObjectPath) ParseFileUrl(string fileUrl)
    {
        var value = fileUrl;
        const string marker = "/storage/v1/object/";
        var idx = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            value = value[(idx + marker.Length)..];
            if (value.StartsWith("public/", StringComparison.OrdinalIgnoreCase))
                value = value["public/".Length..];
        }

        value = value.TrimStart('/');
        var slash = value.IndexOf('/');
        return slash <= 0 ? (null, string.Empty) : (value[..slash], value[(slash + 1)..]);
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