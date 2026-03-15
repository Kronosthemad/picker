using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Picker.Models;

namespace Picker;

public class MotionService
{
    private string currentPath;
    private List<FileEntry> files = new();
    private int selectedIndex = 0;
    private int maxVisibleItems;
    private bool expectSecondG = false;
    private readonly Action<string> loadDirectory;
    private readonly Action<string> openFile;

    public string CurrentPath => currentPath;
    public int SelectedIndex => selectedIndex;
    public int FileCount => files.Count;

    public MotionService(Action<string> loadDirectory, Action<string> openFile)
    {
        this.loadDirectory = loadDirectory;
        this.openFile = openFile;
        
        currentPath = Environment.CurrentDirectory;
        int windowHeight;
        try { windowHeight = Console.WindowHeight; }
        catch { windowHeight = 24; }
        maxVisibleItems = Math.Max(10, windowHeight - 4);
    }

    public void UpdateFiles(List<FileEntry> newFiles)
    {
        files = newFiles;
        selectedIndex = 0;
    }

    public void MoveUp()
    {
        if (selectedIndex > 0) selectedIndex--;
    }

    public void MoveDown()
    {
        if (selectedIndex < files.Count - 1) selectedIndex++;
    }

    public void MoveIn()
    {
        if (files.Count > 0)
        {
            var selected = files[selectedIndex];
            if (selected.IsDirectory)
            {
                loadDirectory(selected.FullPath);
            }
            else
            {
                openFile(selected.FullPath);
            }
        }
    }

    public void MoveOut()
    {
        var parent = Directory.GetParent(currentPath);
        if (parent != null)
        {
            loadDirectory(parent.FullName);
        }
    }

    public void MoveToStart()
    {
        selectedIndex = 0;
    }

    public void MoveToEnd()
    {
        selectedIndex = Math.Max(0, files.Count - 1);
    }

    public void MovePageUp()
    {
        selectedIndex = Math.Max(0, selectedIndex - maxVisibleItems);
    }

    public void MovePageDown()
    {
        selectedIndex = Math.Min(files.Count - 1, selectedIndex + maxVisibleItems);
    }

    public bool HandleDoubleG()
    {
        if (expectSecondG)
        {
            selectedIndex = Math.Max(0, files.Count - 1);
            expectSecondG = false;
            return true;
        }
        expectSecondG = true;
        return false;
    }

    public void ResetDoubleG()
    {
        expectSecondG = false;
    }

    public void SetPath(string path)
    {
        currentPath = path;
    }

    public FileEntry? GetSelectedFile()
    {
        if (files.Count > 0 && selectedIndex < files.Count)
            return files[selectedIndex];
        return null;
    }
}
