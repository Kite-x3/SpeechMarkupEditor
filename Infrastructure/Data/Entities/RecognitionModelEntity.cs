namespace SpeechMarkupEditor.Infrastructure.Data.Entities;

public class RecognitionModelEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Engine { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
}
