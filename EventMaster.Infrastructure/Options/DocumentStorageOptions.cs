namespace EventMaster.Infrastructure.Options;

public class DocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";

    /// <summary>Absolute or app-relative path for uploads (under web root).</summary>
    public string PostersRelativePath { get; set; } = "uploads/posters";
}
