namespace EngineeringSystemsDesk.Models;

public class Comment
{
    public string Author { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public override string ToString() => $"[{CreatedAt:yyyy-MM-dd HH:mm}] {Author}: {Text}";
}
