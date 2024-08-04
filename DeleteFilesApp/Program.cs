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
        string backupFolderPath = null;
        int keepFileCount = 0;
        bool isConfigFileSpecified = false;
        string configFilePath = null;
        bool createConfig = false;

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
                case "-b":
                    if (i + 1 < args.Length)
                    {
                        backupFolderPath = args[++i];
                    }
                    break;
                case "-createconfig":
                    createConfig = true;
                    break;
                default:
                    break;
            }
        }


        if (string.IsNullOrEmpty(rootFolderPath) || string.IsNullOrEmpty(backupFolderPath))
        {
            LogMessage("log.txt", "Root folder path or backup folder path is not specified.");
            Console.WriteLine("Usage: deletefiles.exe -p \"rootFolderPath\" -c keepFileCount -b \"backupFolderPath\" [-cf configFilePath]");
            return;
        }

        if (keepFileCount <= 0)
        {
            LogMessage("log.txt", "Keep file count must be greater than 0.");
            Console.WriteLine("Usage: deletefiles.exe -p \"rootFolderPath\" -c keepFileCount -b \"backupFolderPath\" [-cf configFilePath]");

            return;
        }

        if (createConfig)
        {
            if (string.IsNullOrEmpty(rootFolderPath) || keepFileCount <= 0)
            {
                Console.WriteLine("Usage: deletefiles.exe -p \"rootFolderPath\" -c keepFileCount -createconfig");
                return;
            }

            GenerateConfigFile(rootFolderPath, keepFileCount);
            Console.WriteLine("Config file created successfully.");
            return;
        }

        if (!Directory.Exists(rootFolderPath))
        {
            LogMessage("log.txt", "Directory does not exist.");
            Console.WriteLine("Usage: deletefiles.exe -p \"rootFolderPath\" -c keepFileCount -b \"backupFolderPath\" [-cf configFilePath]");
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
                ProcessFolders(rootFolderPath, keepFileCount,backupFolderPath,config);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load configuration file: {ex.Message}");
                return;
            }
        }
        else
        {
            
            ProcessFolders(rootFolderPath, keepFileCount,backupFolderPath);
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

    static void ProcessFolders(string rootFolderPath, int keepFileCount, string backupFolderPath, MainConfig config = null)
    {
        var folders = Directory.GetDirectories(rootFolderPath, "*", SearchOption.AllDirectories);
        foreach (var folder in folders)
        {
            int currentKeepFileCount = keepFileCount;

            if (config != null)
            {
                var folderConfig = config.FolderList.FirstOrDefault(f => f.FolderName.Equals(folder, StringComparison.OrdinalIgnoreCase));
                if (folderConfig != null)
                {
                    currentKeepFileCount = folderConfig.KeepFileCount;
                }
            }

            DeleteFilesInFolder(folder, currentKeepFileCount, backupFolderPath);
        }
    }


    static void DeleteFilesInFolder(string folder, int keepFileCount, string backupFolderPath)
    {
        var files = Directory.GetFiles(folder, "*.bak")
                         .Select(f => new FileInfo(f))
                         .OrderByDescending(f => f.LastWriteTime)
                         .ToList();
        for (int i = keepFileCount; i < files.Count; i++)
        {
            try
            {
                string destinationPath = Path.Combine(backupFolderPath, files[i].Name);
                File.Copy(files[i].FullName, destinationPath, true);
                files[i].Delete();
                LogMessage("log.txt", $"File copied and deleted: {files[i].FullName} -> {destinationPath}");
            }
            catch (Exception ex)
            {
                LogMessage("log.txt", $"Failed to delete file: {files[i].FullName}. Error: {ex.Message}");
            }
        }
    }

    static void GenerateConfigFile(string rootFolderPath, int keepFileCount)
    {
        var config = new MainConfig
        {
            RootPath = rootFolderPath,
            FolderList = new List<FolderInfo>()
        };

        var folders = Directory.GetDirectories(rootFolderPath, "*", SearchOption.AllDirectories);
        foreach (var folder in folders)
        {
            var bakFiles = Directory.GetFiles(folder, "*.bak");
            if (bakFiles.Length > 0)
            {
                config.FolderList.Add(new FolderInfo
                {
                    FolderName = folder,
                    KeepFileCount = keepFileCount
                });
            }
        }

        string configFilePath = Path.Combine(rootFolderPath, "config.json");
        File.WriteAllText(configFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        LogMessage("log.txt", $"Config file created at: {configFilePath}");
    }

    static void LogMessage(string logFilePath, string message)
    {
        File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
    }
}



