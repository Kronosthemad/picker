namespace Picker.Models;

public enum BookmarkType
{
    Project,
    Regular
}

public class BookmarkEntry
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public BookmarkType Type { get; set; }
}
