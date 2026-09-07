namespace MediSphere.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string folder);
    Task DeleteAsync(string fileUrl);
    Task<(Stream Stream, string ContentType)?> GetFileAsync(string fileUrl);
}
