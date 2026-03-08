using Spectre.Console;
using System.Runtime.InteropServices;

namespace Picker;

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
