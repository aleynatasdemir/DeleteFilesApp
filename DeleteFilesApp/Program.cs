using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public class MainConfig
{
    public string RootPath { get; set; }
    public List<FolderInfo> FolderList { get; set; }
}

public class FolderInfo
{
    public string FolderName { get; set; }
    public int KeepFileCount { get; set; }
}

class Program
{
    static void Main(string[] args)
    {
        string rootFolderPath = null;
        int keepFileCount = 0;
        bool isConfigFileSpecified = false;
        string configFilePath = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-p":
                    if (i + 1 < args.Length)
                    {
                        rootFolderPath = args[++i];
                    }
                    break;
                case "-c":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out keepFileCount))
                    {
                        isConfigFileSpecified = false;
                    }
                    break;
                case "-cf":
                    if (i + 1 < args.Length)
                    {
                        configFilePath = args[++i];
                        isConfigFileSpecified = true;
                    }
                    break;
                default:
                    break;
            }
        }

        
        if (string.IsNullOrEmpty(rootFolderPath))
        {
            LogMessage("log.txt", "Root folder path is not specified.");
            Console.WriteLine("Usage: deletefiles.exe -p \"rootFolderPath\" -c keepFileCount [-cf configFilePath]");
            return;
        }

        if (keepFileCount <= 0)
        {
            LogMessage("log.txt", "Keep file count must be greater than 0.");
            Console.WriteLine("Usage: deletefiles.exe -p \"rootFolderPath\" -c keepFileCount [-cf configFilePath]");
            return;
        }

        if (!Directory.Exists(rootFolderPath))
        {
            LogMessage("log.txt", "Directory does not exist.");
            Console.WriteLine("Usage: deletefiles.exe -p \"rootFolderPath\" -c keepFileCount [-cf configFilePath]");
            return;
        }

        if ((isConfigFileSpecified && keepFileCount <= 0) || (rootFolderPath == null && isConfigFileSpecified) || (rootFolderPath != null && keepFileCount <= 0))
        {
            LogMessage("log.txt", "Invalid argument combinations.");
            return;
        }

        if (isConfigFileSpecified)
        {
            try
            {
                var config = LoadConfig(configFilePath);
                ProcessFolders(rootFolderPath, keepFileCount, config);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load configuration file: {ex.Message}");
                return;
            }
        }
        else
        {
            
            ProcessFolders(rootFolderPath, keepFileCount);
        }
    }

    static MainConfig LoadConfig(string configFilePath)
    {
        if (string.IsNullOrEmpty(configFilePath))
        {
            LogMessage("log.txt", "Config file path is not specified.");
            throw new ArgumentException("Config file path is not specified.");
        }

        if (!File.Exists(configFilePath))
        {
            LogMessage("log.txt", "Config file does not exist.");
            throw new FileNotFoundException("Config file does not exist.", configFilePath);
        }

        try
        {
            string configContent = File.ReadAllText(configFilePath);
            return JsonConvert.DeserializeObject<MainConfig>(configContent);
        }
        catch (Exception ex)
        {
            LogMessage("log.txt", $"Error loading config file: {ex.Message}");
            throw;
        }
    }

    static void ProcessFolders(string rootFolderPath, int keepFileCount, MainConfig config = null)
    {
        var folders = Directory.GetDirectories(rootFolderPath, "*", SearchOption.AllDirectories);
        foreach (var folder in folders)
        {
            int currentKeepFileCount = keepFileCount;
            if (config != null)
            {
                var folderConfig = config.FolderList.FirstOrDefault(f => f.FolderName.Equals(Path.GetFileName(folder), StringComparison.OrdinalIgnoreCase));
                if (folderConfig != null)
                {
                    currentKeepFileCount = folderConfig.KeepFileCount;
                }
            }
            DeleteFilesInFolder(folder, currentKeepFileCount);
        }
    }

    static void DeleteFilesInFolder(string folder, int keepFileCount)
    {
        var files = Directory.GetFiles(folder).Select(f => new FileInfo(f)).OrderByDescending(f => f.LastWriteTime).ToList();
        for (int i = keepFileCount; i < files.Count; i++)
        {
            files[i].Delete();
        }
    }

    static void LogMessage(string logFilePath, string message)
    {
        File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
    }
}



