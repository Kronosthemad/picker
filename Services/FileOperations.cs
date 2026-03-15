using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Picker.Models;

namespace Picker;

public class FileOperations
{
    private readonly string currentPath;
    private readonly Action<string> loadDirectory;

    public FileOperations(string currentPath, Action<string> loadDirectory)
    {
        this.currentPath = currentPath;
        this.loadDirectory = loadDirectory;
    }

    public void SetPath(string path)
    {
        var field = typeof(FileOperations).GetField("currentPath", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(this, path);
    }

    public void NewPrompt()
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

    public void DeletePrompt(List<FileEntry> files, int selectedIndex)
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

    public void NewFile(string name)
    {
        var newPath = Path.Combine(currentPath, name);
        try
        {
            if (!File.Exists(newPath))
            {
                File.WriteAllText(newPath, "");
                loadDirectory(currentPath);
            }
        }
        catch { }
    }

    public void DeleteFile(FileEntry file)
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
            loadDirectory(currentPath);
        }
        catch { }
    }

    public void OpenFile(string path)
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
        catch { }
    }
}
