using System;
using System.IO;
using System.Text;

namespace Picker;

public class FileIconProvider
{
    private readonly bool useEmoji;

    public FileIconProvider()
    {
        useEmoji = CheckEmojiSupported();
    }

    private bool CheckEmojiSupported()
    {
        try
        {
            var enc = Console.OutputEncoding ?? Encoding.UTF8;
            var sample = "📁";
            var bytes = enc.GetBytes(sample);
            var round = enc.GetString(bytes);
            return round == sample;
        }
        catch
        {
            return false;
        }
    }

    public string GetFileIcon(string name, bool isDirectory)
    {
        if (isDirectory) return useEmoji ? "📁" : "▸";

        var ext = Path.GetExtension(name).ToLowerInvariant();

        if (string.IsNullOrEmpty(ext))
            return useEmoji ? "📄" : "•";

        return ext switch
        {
            ".md" => useEmoji ? "📝" : "M",
            ".txt" => useEmoji ? "📄" : "T",
            ".cs" or ".js" or ".ts" or ".cpp" or ".c" or ".h" or ".java" or ".py" => useEmoji ? "🛠️" : "C",
            ".json" or ".xml" or ".yaml" or ".yml" => useEmoji ? "🔩" : "J",
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".svg" => useEmoji ? "🖼️" : "I",
            ".zip" or ".tar" or ".gz" or ".rar" or ".7z" => useEmoji ? "📦" : "Z",
            ".exe" or ".dll" or ".so" => useEmoji ? "⚙️" : "X",
            ".mp3" or ".wav" or ".flac" => useEmoji ? "🎵" : "A",
            ".mp4" or ".mkv" or ".avi" or ".mov" => useEmoji ? "🎬" : "V",
            ".pdf" => useEmoji ? "📕" : "P",
            _ => useEmoji ? "📄" : "•",
        };
    }

    public string GetFolderIcon() => useEmoji ? "📁" : "▸";
}
