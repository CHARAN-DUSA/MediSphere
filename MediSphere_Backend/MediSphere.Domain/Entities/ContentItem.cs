using MediSphere.Domain.Common;

namespace MediSphere.Domain.Entities;

public class ContentItem : BaseEntity
{
    public string Type { get; set; } = string.Empty; // FAQ, HealthArticle, Banner
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int Order { get; set; } = 0;
}
