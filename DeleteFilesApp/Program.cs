using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        string rootFolderPath = string.Empty;
        int keepFileCount = 0;
        string logFilePath = $"{DateTime.Now:yyyy-MM-dd}_log.txt";

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-p":
                    if (i + 1 < args.Length)
                    {
                        rootFolderPath = args[i + 1];
                    }
                    break;
                case "-c":
                    if (i + 1 < args.Length)
                    {
                        if (int.TryParse(args[i + 1], out int count))
                        {
                            keepFileCount = count;
                        }
                    }
                    break;
            }
        }

        if (string.IsNullOrEmpty(rootFolderPath))
        {
            LogMessage(logFilePath, "Root folder path is not specified.");
            Console.WriteLine("Usage: deletefiles.exe -p \"rootFolderPath\" -c keepFileCount");
            return;
        }

        if (keepFileCount <= 0)
        {
            LogMessage(logFilePath, "Keep file count must be a positive integer.");
            Console.WriteLine("Usage: deletefiles.exe -p \"rootFolderPath\" -c keepFileCount");
            return;
        }

        if (!Directory.Exists(rootFolderPath))
        {
            LogMessage(logFilePath, "The specified directory does not exist.");
            Console.WriteLine("Usage: deletefiles.exe -p \"rootFolderPath\" -c keepFileCount");
            return;
        }

        ProcessDirectory(rootFolderPath, keepFileCount, logFilePath);
    }

    static void ProcessDirectory(string currentFolder, int keepFileCount, string logFilePath)
    {
        try
        {
            var bakFiles = new DirectoryInfo(currentFolder).GetFiles("*.bak").OrderBy(f => f.LastWriteTime).ToList();

            foreach (var file in bakFiles.Skip(keepFileCount))
            {
                file.Delete();
                LogDeletedFile(file.FullName, logFilePath);
            }

            foreach (var dir in Directory.GetDirectories(currentFolder))
            {
                ProcessDirectory(dir, keepFileCount, logFilePath); 
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            LogError(ex.Message, logFilePath);
        }
        catch (Exception ex)
        {
            LogError(ex.Message, logFilePath);
        }
    }

    static void LogDeletedFile(string filePath, string logFilePath)
    {
        try
        {
            File.AppendAllText(logFilePath, $"{DateTime.Now}: Deleted {filePath}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to log deleted file: {ex.Message}");
        }
    }

    static void LogError(string message, string logFilePath)
    {
        try
        {
            File.AppendAllText(logFilePath, $"{DateTime.Now}: Error {message}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to log error: {ex.Message}");
        }
    }

    static void LogMessage(string logFilePath, string message)
    {
        try
        {
            File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to log message: {ex.Message}");
        }
    }
}

