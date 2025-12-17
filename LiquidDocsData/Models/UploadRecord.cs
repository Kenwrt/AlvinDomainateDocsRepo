namespace LiquidDocsData.Models;

public record UploadRecord
{
    public string FileName { get; init; }

    public string FileData { get; init; }
}