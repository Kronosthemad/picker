using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Picker;

public class FileEntry
{
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public bool IsDirectory { get; set; }
    public long Size { get; set; }
    public DateTime Modified { get; set; }
}


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

public class FileManager
{
    private string currentPath;
    private List<FileEntry> files = new();
    private List<BookmarkEntry> bookmarks = new();
    private int selectedIndex = 0;
    private readonly int maxVisibleItems;
    private bool expectSecondG = false;
    private readonly bool useEmoji;
    private readonly string bookmarksFile;

    public FileManager()
    {
        currentPath = Environment.CurrentDirectory;
        int windowHeight;
        try { windowHeight = Console.WindowHeight; }
        catch { windowHeight = 24; }
        maxVisibleItems = Math.Max(10, windowHeight - 4);
        
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var pickerDir = Path.Combine(appData, "picker");
        Directory.CreateDirectory(pickerDir);
        bookmarksFile = Path.Combine(pickerDir, "bookmarks.json");
        useEmoji = CheckEmojiSupported();
        LoadBookmarks();
    }

    private bool CheckEmojiSupported()
    {
        try
        {
            var enc = Console.OutputEncoding ?? System.Text.Encoding.UTF8;
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

    private string GetFileIcon(string name, bool isDirectory)
    {
        if (isDirectory) return useEmoji ? "📁" : "▸";

        var ext = Path.GetExtension(name).ToLowerInvariant();

        if (string.IsNullOrEmpty(ext))
            return useEmoji ? "📄" : "•";

        return ext switch
        {
            ".md" => useEmoji ? "📝" : "M",
            ".txt" => useEmoji ? "📄" : "T",
            ".cs" or ".js" or ".ts" or ".cpp" or ".c" or ".h" or ".java" or ".py" => useEmoji ? "💻" : "C",
            ".json" or ".xml" or ".yaml" or ".yml" => useEmoji ? "🧾" : "J",
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".svg" => useEmoji ? "🖼️" : "I",
            ".zip" or ".tar" or ".gz" or ".rar" or ".7z" => useEmoji ? "📦" : "Z",
            ".exe" or ".dll" or ".so" => useEmoji ? "⚙️" : "X",
            ".mp3" or ".wav" or ".flac" => useEmoji ? "🎵" : "A",
            ".mp4" or ".mkv" or ".avi" or ".mov" => useEmoji ? "🎬" : "V",
            ".pdf" => useEmoji ? "📕" : "P",
            _ => useEmoji ? "📄" : "•",
        };
    }

    public void Run()
    {
        LoadDirectory(currentPath);

        while (true)
        {
            AnsiConsole.Clear();
            Render();
            
            var key = Console.ReadKey(true);
            if (!HandleInput(key)) break;
        }
    }

    private void LoadDirectory(string path)
    {
        files.Clear();
        
        try
        {
            var dirInfo = new DirectoryInfo(path);
            
            if (dirInfo.Parent != null)
            {
                files.Add(new FileEntry
                {
                    Name = "..",
                    FullPath = dirInfo.Parent.FullName,
                    IsDirectory = true
                });
            }

            foreach (var dir in dirInfo.GetDirectories().OrderBy(d => d.Name))
            {
                files.Add(new FileEntry
                {
                    Name = dir.Name,
                    FullPath = dir.FullName,
                    IsDirectory = true,
                    Modified = dir.LastWriteTime
                });
            }

            foreach (var file in dirInfo.GetFiles().OrderBy(f => f.Name))
            {
                files.Add(new FileEntry
                {
                    Name = file.Name,
                    FullPath = file.FullName,
                    IsDirectory = false,
                    Size = file.Length,
                    Modified = file.LastWriteTime
                });
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
        }

        currentPath = path;
        selectedIndex = 0;
    }

    private bool HandleInput(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
            case ConsoleKey.K:
                if (selectedIndex > 0) selectedIndex--;
                return true;
                
            case ConsoleKey.DownArrow:
            case ConsoleKey.J:
                if (selectedIndex < files.Count - 1) selectedIndex++;
                return true;
                
            case ConsoleKey.Enter:
            case ConsoleKey.L:
                if (files.Count > 0)
                {
                    var selected = files[selectedIndex];
                    if (selected.IsDirectory)
                    {
                        LoadDirectory(selected.FullPath);
                    }
                    else
                    {
                        OpenFile(selected.FullPath);
                    }
                }
                return true;
                
            case ConsoleKey.Backspace:
            case ConsoleKey.H:
                var parent = Directory.GetParent(currentPath);
                if (parent != null)
                {
                    LoadDirectory(parent.FullName);
                }
                return true;
                
            case ConsoleKey.Q:
                return false;
                
            case ConsoleKey.Home:
                selectedIndex = 0;
                return true;
                
            case ConsoleKey.End:
                selectedIndex = Math.Max(0, files.Count - 1);
                return true;
                
            case ConsoleKey.PageUp:
                selectedIndex = Math.Max(0, selectedIndex - maxVisibleItems);
                return true;
                
            case ConsoleKey.PageDown:
                selectedIndex = Math.Min(files.Count - 1, selectedIndex + maxVisibleItems);
                return true;
                
            case ConsoleKey.G:
                if (expectSecondG)
                {
                    selectedIndex = Math.Max(0, files.Count - 1);
                    expectSecondG = false;
                }
                else
                {
                    expectSecondG = true;
                }
                return true;
                
            case ConsoleKey.M:
                AddBookmark();
                return true;
                
            case ConsoleKey.B:
                SelectBookmark();
                return true;
                
            default:
                expectSecondG = false;
                return true;
        }
    }

    private void OpenFile(string path)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(startInfo);
        }
        catch
        {
        }
    }

    private void LoadBookmarks()
    {
        try
        {
            if (File.Exists(bookmarksFile))
            {
                var json = File.ReadAllText(bookmarksFile);
                bookmarks = JsonSerializer.Deserialize<List<BookmarkEntry>>(json) ?? new();
            }
        }
        catch
        {
            bookmarks = new();
        }
    }

    private void SaveBookmarks()
    {
        try
        {
            var json = JsonSerializer.Serialize(bookmarks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(bookmarksFile, json);
        }
        catch { }
    }

    private void AddBookmark()
    {
        var dirName = Path.GetFileName(currentPath);
        if (string.IsNullOrEmpty(dirName)) dirName = currentPath;
        
        if (bookmarks.Any(b => b.Path == currentPath))
        {
            return;
        }

        Console.WriteLine("Add bookmark?");
        Console.WriteLine("p: Project | r: Regular | any key: Cancel");
        
        var key = Console.ReadKey(true);
        BookmarkType type;
        
        switch (key.Key)
        {
            case ConsoleKey.P:
                type = BookmarkType.Project;
                break;
            case ConsoleKey.R:
                type = BookmarkType.Regular;
                break;
            default:
                return;
        }

        bookmarks.Add(new BookmarkEntry
        {
            Name = dirName,
            Path = currentPath,
            Type = type
        });
        SaveBookmarks();
    }

    private bool SelectBookmark()
    {
        if (bookmarks.Count == 0)
        {
            Console.WriteLine("No bookmarks yet. Press 'm' to add one.");
            Thread.Sleep(1000);
            return true;
        }

        var projectBookmarks = bookmarks.Where(b => b.Type == BookmarkType.Project).ToList();
        var regularBookmarks = bookmarks.Where(b => b.Type == BookmarkType.Regular).ToList();

        var allBookmarks = new List<BookmarkEntry>();
        
        if (projectBookmarks.Any())
        {
            allBookmarks.AddRange(projectBookmarks);
        }
        if (regularBookmarks.Any())
        {
            allBookmarks.AddRange(regularBookmarks);
        }

        int bookmarkIndex = 0;
        
        while (true)
        {
            Console.Clear();

            Console.WriteLine(" Bookmarks - j/k: navigate | Enter: select | d: delete | Esc: cancel");
            Console.WriteLine(new string('-', 50));
            
            for (int i = 0; i < allBookmarks.Count; i++)
            {
                var isSelected = i == bookmarkIndex;
                var prefix = isSelected ? "> " : "  ";
                var name = allBookmarks[i].Name;
                if (allBookmarks[i].Type == BookmarkType.Project)
                {
                    name = $"[P] {name}";
                }
                else
                {
                    name = $"[R] {name}";
                }
                Console.WriteLine(prefix + name);
            }
            Console.WriteLine(new string('-', 50));

            var key = Console.ReadKey(true);
            
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.K:
                    if (bookmarkIndex > 0) bookmarkIndex--;
                    else bookmarkIndex = allBookmarks.Count - 1;
                    break;
                case ConsoleKey.DownArrow:
                case ConsoleKey.J:
                    if (bookmarkIndex < allBookmarks.Count - 1) bookmarkIndex++;
                    else bookmarkIndex = 0;
                    break;
                case ConsoleKey.Enter:
                    var bookmark = allBookmarks[bookmarkIndex];
                    if (Directory.Exists(bookmark.Path))
                    {
                        LoadDirectory(bookmark.Path);
                        return true;
                    }
                    break;
                case ConsoleKey.D:
                case ConsoleKey.Delete:
                    // Confirm deletion
                    var toRemove = allBookmarks[bookmarkIndex];
                    Console.Clear();
                    Console.WriteLine($"Delete bookmark '{toRemove.Name}'? (y/n)");
                    var confirm = Console.ReadKey(true);
                    if (confirm.Key == ConsoleKey.Y)
                    {
                        bookmarks.RemoveAll(b => b.Path == toRemove.Path);
                        SaveBookmarks();

                        // Rebuild lists
                        projectBookmarks = bookmarks.Where(b => b.Type == BookmarkType.Project).ToList();
                        regularBookmarks = bookmarks.Where(b => b.Type == BookmarkType.Regular).ToList();
                        allBookmarks = new List<BookmarkEntry>();
                        if (projectBookmarks.Any()) allBookmarks.AddRange(projectBookmarks);
                        if (regularBookmarks.Any()) allBookmarks.AddRange(regularBookmarks);

                        if (allBookmarks.Count == 0)
                        {
                            Console.WriteLine("No bookmarks left. Press any key...");
                            Console.ReadKey(true);
                            return true;
                        }

                        if (bookmarkIndex >= allBookmarks.Count) bookmarkIndex = allBookmarks.Count - 1;
                    }
                    break;
                case ConsoleKey.Escape:
                    return true;
            }
        }
    }

    private void Render()
    {
        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Status").Size(1),
                new Layout("Content"));

        layout["Status"].Update(new Panel(
            Markup.Escape(currentPath))
            .Padding(0, 0, 0, 0)
            .Border(BoxBorder.None));

        layout["Content"].SplitColumns(
            new Layout("Tree").Size(25),
            new Layout("Files"),
            new Layout("Preview").Size(30));

        var tree = BuildDirectoryTree();
        layout["Tree"].Update(new Panel(tree)
            .Border(BoxBorder.None)
            .Padding(0, 0, 0, 0));

        var fileList = BuildFileList();
        layout["Files"].Update(new Panel(fileList)
            .Border(BoxBorder.None)
            .Padding(0, 0, 0, 0));

        var preview = BuildPreview();
        layout["Preview"].Update(new Panel(preview)
            .Border(BoxBorder.None)
            .Padding(0, 0, 0, 0));

        AnsiConsole.Write(layout);
    }

    private IRenderable BuildDirectoryTree()
    {
        var tree = new Tree(currentPath);
        var rootDir = new DirectoryInfo(currentPath);
        
        try
        {
            var subdirs = rootDir.GetDirectories().Take(5).ToList();
            foreach (var dir in subdirs)
            {
                // prepend folder icon (emoji or ASCII)
                var folderIcon = useEmoji ? "📁" : "▸";
                tree.AddNode($"[blue]{folderIcon} {dir.Name}/[/]");
            }
        }
        catch { }

        return tree;
    }

    private IRenderable BuildFileList()
    {
        var table = new Table();
        table.AddColumn(new TableColumn("Name").Width(30));
        
        foreach (var file in files)
        {
            var isSelected = files.IndexOf(file) == selectedIndex;
            var style = isSelected ? "[white on blue]" : "";
            var endStyle = isSelected ? "[/]" : "";
            
            string name;
            var icon = GetFileIcon(file.Name, file.IsDirectory);
            if (file.IsDirectory)
            {
                name = $"{style}[blue]{icon} {file.Name}/[/]{endStyle}";
            }
            else
            {
                name = $"{style}{icon} {file.Name}{endStyle}";
            }
            
            table.AddRow(name);
        }
        
        return table;
    }

    private IRenderable BuildPreview()
    {
        if (files.Count == 0 || selectedIndex >= files.Count)
            return new Text("No file selected");

        var selected = files[selectedIndex];
        var icon = GetFileIcon(selected.Name, selected.IsDirectory);

        if (selected.IsDirectory)
        {
            return new Text($"[blue]{icon} Directory: {selected.Name}[/]");
        }

        try
        {
            var ext = Path.GetExtension(selected.FullPath).ToLower();
            var textExts = new[] { ".txt", ".md", ".cs", ".json", ".xml", ".html", ".css", ".js", ".cpp", ".ts", ".yaml", ".yml", ".ini", ".cfg", ".log", ".sh", ".bat", ".ps1" };
            
            if (textExts.Contains(ext) || selected.Size < 100_000)
            {
                var lines = File.ReadLines(selected.FullPath).Take(50).ToList();
                var content = string.Join("\n", lines);
                // show header with icon + filename
                var header = $"{icon} {selected.Name}\n\n";
                return new Text(header + content);
            }
            else
            {
                return new Text($"[yellow]{icon} File: {selected.Name}\nSize: {selected.Size:N0} bytes\nModified: {selected.Modified:g}[/]");
            }
        }
        catch (Exception ex)
        {
            return new Text($"[red]{ex.Message}[/]");
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        // Try set console code page to UTF-8 on Windows, then use UTF-8 encodings.
        // This helps avoid '??' when printing emoji on Windows consoles.
        try { SetConsoleOutputCP(65001); } catch { }
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        AnsiConsole.MarkupLine("[bold cyan]Picker - File Manager[/]");
        AnsiConsole.MarkupLine("[dim]h/j/k/l: vim nav | Enter: open | Backspace: parent | m: bookmark | b: bookmarks | Q: quit[/]");
        
        Thread.Sleep(500);
        
        var fm = new FileManager();
        fm.Run();
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleOutputCP(uint wCodePageID);
}
