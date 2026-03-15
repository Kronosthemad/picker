using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using Picker.Models;

namespace Picker;

public class FileManager
{
    private string currentPath;
    private List<FileEntry> files = new();
    private BookmarkService bookmarkService;
    private List<BookmarkEntry> bookmarks => bookmarkService.Bookmarks;
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
        bookmarkService = new BookmarkService(bookmarksFile);
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

    private void MoveIn()
    {
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
    }

    private void MoveOut()
    {
        var parent = Directory.GetParent(currentPath);
        if (parent != null)
        {
            LoadDirectory(parent.FullName);
        }
    }

    private void NewPrompt()
    {
        Console.Clear();
        Console.WriteLine("Enter name for new file (Esc to cancel):");
        var name = "";
        while (true)
        {
            var inputKey = Console.ReadKey(true);
            if (inputKey.Key == ConsoleKey.Enter)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    NewFile(name);
                }
                break;
            }
            else if (inputKey.Key == ConsoleKey.Backspace)
            {
                if (name.Length > 0)
                {
                    name = name.Substring(0, name.Length - 1);
                    Console.Write("\b \b");
                }
            }
            else if (inputKey.Key == ConsoleKey.Escape)
            {
                break;
            }
            else
            {
                if (!char.IsControl(inputKey.KeyChar))
                {
                    name += inputKey.KeyChar;
                    Console.Write(inputKey.KeyChar);
                }
            }
        }
    }

    private void DeletePrompt()
    {
        if (files.Count > 0)
        {
            var toDelete = files[selectedIndex];
            Console.Clear();
            Console.WriteLine($"Delete '{toDelete.Name}'? (y/n)");
            var confirm = Console.ReadKey(true);
            if (confirm.Key == ConsoleKey.Y)
            {
                DeleteFile(toDelete);
            }
        }
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
                MoveIn();
                return true;
                
            case ConsoleKey.Backspace:
            case ConsoleKey.H:
                MoveOut();
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

            case ConsoleKey.N:
                // Prompt for new file name (Esc cancels)
                NewPrompt();
                return true;

            case ConsoleKey.Delete:
            case ConsoleKey.D:
                DeletePrompt();
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

    private void  NewFile(string name)
    {
        var newPath = Path.Combine(currentPath, name);
        try
        {
            if (!File.Exists(newPath))
            {
                File.WriteAllText(newPath, "");
                LoadDirectory(currentPath);
                selectedIndex = files.FindIndex(f => f.FullPath == newPath);
            }
        }
        catch
        {
            ErrorEventArgs e = new ErrorEventArgs(new Exception("Failed to create file."));
        }
    }

    private void DeleteFile(FileEntry file)
    {
        try
        {
            if (file.IsDirectory)
            {
                Directory.Delete(file.FullPath, true);
            }
            else
            {
                File.Delete(file.FullPath);
            }
            LoadDirectory(currentPath);
        }
        catch
        {
            ErrorEventArgs e = new ErrorEventArgs(new Exception("Failed to delete."));
        }
    }

    // Bookmark persistence handled by BookmarkService

    private void AddBookmark()
    {
        var dirName = Path.GetFileName(currentPath);
        if (string.IsNullOrEmpty(dirName)) dirName = currentPath;
        
        if (bookmarks.Any(b => b.Path == currentPath)) return;

        Console.WriteLine("Add bookmark?");
        Console.WriteLine("p: Project | r: Regular | any key: Cancel");

        var key = Console.ReadKey(true);
        BookmarkType type;
        switch (key.Key)
        {
            case ConsoleKey.P: type = BookmarkType.Project; break;
            case ConsoleKey.R: type = BookmarkType.Regular; break;
            default: return;
        }

        bookmarkService.Add(dirName, currentPath, type);
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
                    name = $"[⚙️] {name}";
                }
                else
                {
                    name = $"[📖] {name}";
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
                        bookmarkService.RemoveByPath(toRemove.Path);

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

                

                case ConsoleKey.E:
                    if (allBookmarks[bookmarkIndex].Type == BookmarkType.Project)
                    {
                        Console.Clear();
                        Console.WriteLine("Edit bookmark name (Esc to cancel):");
                        var newName = "";
                        while (true)
                        {
                            var inputKey = Console.ReadKey(true);
                            if (inputKey.Key == ConsoleKey.Enter)
                            {
                                if (!string.IsNullOrWhiteSpace(newName))
                                {
                                    var bookmarkToEdit = allBookmarks[bookmarkIndex];
                                    bookmarkToEdit.Name = newName;
                                    bookmarkService.Save();
                                }
                                break;
                            }
                            else if (inputKey.Key == ConsoleKey.Backspace)
                            {
                                if (newName.Length > 0)
                                {
                                    newName = newName.Substring(0, newName.Length - 1);
                                    Console.Write("\b \b");
                                }
                            }
                            else if (inputKey.Key == ConsoleKey.Escape)
                            {
                                break;
                            }
                            else
                            {
                                newName += inputKey.KeyChar;
                                Console.Write(inputKey.KeyChar);
                            }
                        }
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
