using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;

class Program
{
    static void Main(string[] args)
    {
        string rootFolderPath = string.Empty;
        string configFilePath = "config.json";
        string logFilePath = $"{DateTime.Now:yyyy-MM-dd}_log.txt";
        bool createConfig = false;

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
                case "-createConfig":
                    createConfig = true;
                    break;
            }
        }

        if (string.IsNullOrEmpty(rootFolderPath))
        {
            LogMessage(logFilePath, "Root folder path is not specified.");
            Console.WriteLine("Usage: deletefiles.exe -p \"rootFolderPath\" [-createConfig]");
            return;
        }

        if (!Directory.Exists(rootFolderPath))
        {
            LogMessage(logFilePath, "The specified directory does not exist.");
            Console.WriteLine("Usage: deletefiles.exe -p \"rootFolderPath\" [-createConfig]");
            return;
        }

        if (createConfig)
        {
            CreateConfigFile(rootFolderPath, configFilePath);
            Console.WriteLine($"Config file created: {configFilePath}. Please update the keep file count and rerun the application.");
        }
        else
        {
            ProcessAndDeleteFiles(configFilePath, logFilePath);
        }
    }

    static void CreateConfigFile(string rootFolderPath, string configFilePath)
    {
        var folderInfos = new List<FolderInfo>();
        ProcessDirectory(rootFolderPath, folderInfos);

        var json = JsonConvert.SerializeObject(folderInfos, Formatting.Indented);
        File.WriteAllText(configFilePath, json);
    }

    static void ProcessDirectory(string currentFolder, List<FolderInfo> folderInfos)
    {
        var bakFiles = new DirectoryInfo(currentFolder).GetFiles("*.bak").OrderBy(f => f.LastWriteTime).ToList();
        folderInfos.Add(new FolderInfo
        {
            FolderPath = currentFolder,
            BakFileCount = bakFiles.Count,
            KeepFileCount = 0 
        });

        foreach (var dir in Directory.GetDirectories(currentFolder))
        {
            ProcessDirectory(dir, folderInfos);
        }
    }

    static void ProcessAndDeleteFiles(string configFilePath, string logFilePath)
    {
        var folderInfos = JsonConvert.DeserializeObject<List<FolderInfo>>(File.ReadAllText(configFilePath));

        foreach (var folderInfo in folderInfos)
        {
            if (folderInfo.KeepFileCount <= 0) continue;

            var bakFiles = new DirectoryInfo(folderInfo.FolderPath).GetFiles("*.bak").OrderBy(f => f.LastWriteTime).ToList();

            foreach (var file in bakFiles.Skip(folderInfo.KeepFileCount))
            {
                var backupFolderPath = Path.Combine(folderInfo.FolderPath, "Backup");
                Directory.CreateDirectory(backupFolderPath);
                var backupFilePath = Path.Combine(backupFolderPath, file.Name);
                file.CopyTo(backupFilePath);
                file.Delete();
                LogDeletedFile(file.FullName, logFilePath);
            }
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

public class FolderInfo
{
    public string FolderPath { get; set; }
    public int BakFileCount { get; set; }
    public int KeepFileCount { get; set; }
}


