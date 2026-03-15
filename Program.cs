using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Picker.Models;

namespace Picker;

class Program
{
    private static FileManager? fileManager;
    private static BookmarkService? bookmarkService;

    static void Main(string[] args)
    {
        try { SetConsoleOutputCP(65001); } catch { }
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        AnsiConsole.MarkupLine("[bold cyan]Picker - File Manager[/]");
        AnsiConsole.MarkupLine("[dim]h/j/k/l: vim nav | Enter: open | Backspace: parent | m: bookmark | b: bookmarks | Q: quit[/]");
        
        Thread.Sleep(500);
        
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var pickerDir = Path.Combine(appData, "picker");
        Directory.CreateDirectory(pickerDir);
        var bookmarksFile = Path.Combine(pickerDir, "bookmarks.json");
        bookmarkService = new BookmarkService(bookmarksFile);
        fileManager = new FileManager();

        Run();
    }

    private static void Run()
    {
        fileManager!.LoadDirectory(Environment.CurrentDirectory);

        while (true)
        {
            AnsiConsole.Clear();
            fileManager.Render();
            
            var key = Console.ReadKey(true);
            if (!HandleInput(key)) break;
        }
    }

    private static bool HandleInput(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
            case ConsoleKey.K:
                fileManager!.MoveUp();
                return true;

            case ConsoleKey.DownArrow:
            case ConsoleKey.J:
                fileManager!.MoveDown();
                return true;

            case ConsoleKey.Enter:
            case ConsoleKey.L:
                fileManager!.MoveIn();
                return true;

            case ConsoleKey.Backspace:
            case ConsoleKey.H:
                fileManager!.MoveOut();
                return true;

            case ConsoleKey.Q:
                return false;

            case ConsoleKey.Home:
                fileManager!.MoveToStart();
                return true;

            case ConsoleKey.End:
                fileManager!.MoveToEnd();
                return true;

            case ConsoleKey.PageUp:
                fileManager!.MovePageUp();
                return true;

            case ConsoleKey.PageDown:
                fileManager!.MovePageDown();
                return true;

            case ConsoleKey.G:
                return !fileManager!.HandleDoubleG();

            case ConsoleKey.M:
                AddBookmark();
                return true;

            case ConsoleKey.B:
                SelectBookmark();
                return true;

            case ConsoleKey.N:
                fileManager!.NewPrompt();
                return true;

            case ConsoleKey.Delete:
            case ConsoleKey.D:
                fileManager!.DeletePrompt();
                return true;

            default:
                fileManager!.ResetDoubleG();
                return true;
        }
    }

    private static void AddBookmark()
    {
        var currentPath = fileManager!.CurrentPath;
        var dirName = Path.GetFileName(currentPath);
        if (string.IsNullOrEmpty(dirName)) dirName = currentPath;
        
        if (bookmarkService!.Bookmarks.Any(b => b.Path == currentPath)) return;

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

    private static void SelectBookmark()
    {
        var bookmarks = bookmarkService!.Bookmarks;
        
        if (bookmarks.Count == 0)
        {
            Console.WriteLine("No bookmarks yet. Press 'm' to add one.");
            Thread.Sleep(1000);
            return;
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
                        fileManager!.LoadDirectory(bookmark.Path);
                        return;
                    }
                    break;
                case ConsoleKey.D:
                case ConsoleKey.Delete:
                    var toRemove = allBookmarks[bookmarkIndex];
                    Console.Clear();
                    Console.WriteLine($"Delete bookmark '{toRemove.Name}'? (y/n)");
                    var confirm = Console.ReadKey(true);
                    if (confirm.Key == ConsoleKey.Y)
                    {
                        bookmarkService.RemoveByPath(toRemove.Path);

                        projectBookmarks = bookmarkService.Bookmarks.Where(b => b.Type == BookmarkType.Project).ToList();
                        regularBookmarks = bookmarkService.Bookmarks.Where(b => b.Type == BookmarkType.Regular).ToList();
                        allBookmarks = new List<BookmarkEntry>();
                        if (projectBookmarks.Any()) allBookmarks.AddRange(projectBookmarks);
                        if (regularBookmarks.Any()) allBookmarks.AddRange(regularBookmarks);

                        if (allBookmarks.Count == 0)
                        {
                            Console.WriteLine("No bookmarks left. Press any key...");
                            Console.ReadKey(true);
                            return;
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
                    return;
            }
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleOutputCP(uint wCodePageID);
}
