namespace MediSphere.Application.DTOs.Admin;

public class SystemSettingDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ContentItemDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty; // FAQ, HealthArticle, Banner
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int Order { get; set; }
}
