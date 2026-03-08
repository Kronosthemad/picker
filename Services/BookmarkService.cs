using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Picker;

using Picker.Models;

public class BookmarkService
{
    private readonly string bookmarksFile;
    public List<BookmarkEntry> Bookmarks { get; private set; } = new();

    public BookmarkService(string bookmarksFile)
    {
        this.bookmarksFile = bookmarksFile;
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(bookmarksFile))
            {
                var json = File.ReadAllText(bookmarksFile);
                Bookmarks = JsonSerializer.Deserialize<List<BookmarkEntry>>(json) ?? new();
            }
            else
            {
                Bookmarks = new List<BookmarkEntry>();
            }
        }
        catch
        {
            Bookmarks = new List<BookmarkEntry>();
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Bookmarks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(bookmarksFile, json);
        }
        catch { }
    }

    public void Add(string name, string path, BookmarkType type)
    {
        if (Bookmarks.Exists(b => b.Path == path)) return;
        Bookmarks.Add(new BookmarkEntry { Name = name, Path = path, Type = type });
        Save();
    }

    public void RemoveByPath(string path)
    {
        Bookmarks.RemoveAll(b => b.Path == path);
        Save();
    }
}
