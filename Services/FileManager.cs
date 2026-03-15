using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Picker.Models;

namespace Picker;

public class FileManager
{
    private readonly FileIconProvider iconProvider;
    private readonly MotionService motion;
    private readonly FileOperations fileOps;
    private List<FileEntry> files = new();

    public string CurrentPath => motion.CurrentPath;

    public FileManager()
    {
        iconProvider = new FileIconProvider();
        motion = new MotionService(LoadDirectory, fileOps_OpenFile);
        fileOps = new FileOperations(Environment.CurrentDirectory, LoadDirectory);
    }

    private void fileOps_OpenFile(string path)
    {
        fileOps.OpenFile(path);
    }

    public void LoadDirectory(string path)
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

        motion.SetPath(path);
        fileOps.SetPath(path);
        motion.UpdateFiles(files);
    }

    public void MoveUp() => motion.MoveUp();
    public void MoveDown() => motion.MoveDown();
    public void MoveIn() => motion.MoveIn();
    public void MoveOut() => motion.MoveOut();
    public void MoveToStart() => motion.MoveToStart();
    public void MoveToEnd() => motion.MoveToEnd();
    public void MovePageUp() => motion.MovePageUp();
    public void MovePageDown() => motion.MovePageDown();
    public bool HandleDoubleG() => motion.HandleDoubleG();
    public void ResetDoubleG() => motion.ResetDoubleG();
    public void NewPrompt() => fileOps.NewPrompt();
    public void DeletePrompt() => fileOps.DeletePrompt(files, motion.SelectedIndex);

    public string GetFileIcon(string name, bool isDirectory) => iconProvider.GetFileIcon(name, isDirectory);
    public string GetFolderIcon() => iconProvider.GetFolderIcon();

    public void Render()
    {
        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Status").Size(1),
                new Layout("Content"));

        layout["Status"].Update(new Panel(
            Markup.Escape(motion.CurrentPath))
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
        var tree = new Tree(motion.CurrentPath);
        var rootDir = new DirectoryInfo(motion.CurrentPath);
        
        try
        {
            var subdirs = rootDir.GetDirectories().Take(15).ToList();
            foreach (var dir in subdirs)
            {
                tree.AddNode($"[blue]{iconProvider.GetFolderIcon()} {dir.Name}/[/]");
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
            var isSelected = files.IndexOf(file) == motion.SelectedIndex;
            var style = isSelected ? "[white on blue]" : "";
            var endStyle = isSelected ? "[/]" : "";
            
            string name;
            var icon = iconProvider.GetFileIcon(file.Name, file.IsDirectory);
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
        var selected = motion.GetSelectedFile();
        if (selected == null)
            return new Text("No file selected");

        var icon = iconProvider.GetFileIcon(selected.Name, selected.IsDirectory);

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
