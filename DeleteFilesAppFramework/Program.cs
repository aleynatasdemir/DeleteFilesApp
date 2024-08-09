using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DeleteFilesAppFramework
{


    public class MainConfig
    {
        //public MainConfig()
        //{
        //    //FolderList = new List<FolderConfig>();
        //}


        public string RootPath { get; set; }
        public int DefaultKeepFileCount { get; set; }
        public string DefaultBackupFolderPath { get; set; }
        public List<FolderConfig> FolderList { get; set; } = new List<FolderConfig>();

    }

    public class FolderConfig
    {
        /// <summary>
        /// foldername sadece root olmadan tutulacak.
        /// 
        /// </summary>
        public string FolderName { get; set; }
        public int KeepFileCount { get; set; }
        public string BackupFolderPath { get; set; }

        ///
        public string GetFullPath(string rootPath)
        {
            return Path.Combine(rootPath, FolderName);
        }
    }

    class Program
    {
        public static bool Test = false;
        private static string logFilePath = $"{DateTime.Now:yyyy-MM-dd}_log.txt";

        static void Main(string[] args)
        {
            var rootFolderPath = string.Empty;
            string backupFolderPath = null;
            int keepFileCount = 0;
            bool isConfigFileSpecified = false;
            string configFilePath = null;
            bool createConfig = false;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLower();
                switch (arg)
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
                        isConfigFileSpecified = true;
                        break;
                    case "-t":
                        Test = true;
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

            var config = new MainConfig();

            if (isConfigFileSpecified)
            {
                try
                {
                    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string configPath = Path.Combine(baseDirectory, "config.json");
                    config = LoadConfig(configPath);
                    ProcessFolders(config);
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load configuration file: {ex.Message}");
                    return;
                }
            }


            if (string.IsNullOrEmpty(rootFolderPath))
            {
                LogMessage("log.txt", "Root folder path is not specified.");
                Console.WriteLine("Usage1: deletefiles.exe -p \"rootFolderPath\" -c keepFileCount [-cf configFilePath]");
                return;
            }

            if (keepFileCount <= 0)
            {
                LogMessage("log.txt", "Keep file count must be greater than 0.");
                Console.WriteLine("Usage2: deletefiles.exe -p \"rootFolderPath\" -c keepFileCount [-cf configFilePath]");
                return;
            }

            if (!Directory.Exists(rootFolderPath))
            {
                LogMessage("log.txt", "Directory does not exist.");
                Console.WriteLine("Usage: deletefiles.exe -p \"rootFolderPath\" -c keepFileCount -b \"backupFolderPath\" [-cf configFilePath]");
                return;
            }


            if (createConfig)
            {
                GenerateConfigFile(rootFolderPath, keepFileCount, backupFolderPath);
                Console.WriteLine("Config file created successfully.");
                return;
            }

            config = new MainConfig()
            {
                RootPath = rootFolderPath,
                DefaultBackupFolderPath = backupFolderPath,
                DefaultKeepFileCount = keepFileCount,
            };
            ProcessFolders(config);
            return;

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

        static void ProcessFolders(MainConfig config)
        {
            var folders = Directory.GetDirectories(config.RootPath, "*", SearchOption.AllDirectories);
            var foldersConfigExsits = config.FolderList.Count > 0;
            foreach (var folder in folders)
            {
                int currentKeepFileCount = config.DefaultKeepFileCount;
                string currentBackupFolderPath = config.DefaultBackupFolderPath;
                if (foldersConfigExsits)
                {

                    var folderConfig = config.FolderList.FirstOrDefault(f => f.GetFullPath(config.RootPath).Equals(folder, StringComparison.OrdinalIgnoreCase));

                    if (folderConfig != null)
                    {
                        currentKeepFileCount = folderConfig.KeepFileCount;
                        if (!string.IsNullOrEmpty(folderConfig.BackupFolderPath))
                        {
                            currentBackupFolderPath = folderConfig.BackupFolderPath;
                        }
                    }
                }

                DeleteFilesInFolder(folder, currentKeepFileCount, currentBackupFolderPath);
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
                    if (!string.IsNullOrEmpty(backupFolderPath))
                    {
                        string destinationPath = Path.Combine(backupFolderPath, files[i].Name);
                        if (Test)
                        {
                            LogMessage("log.txt", $"File copied and deleted (Test): {files[i].FullName} -> {destinationPath}");
                        }
                        else
                        {
                            File.Copy(files[i].FullName, destinationPath, true);
                            files[i].Delete();
                            LogMessage("log.txt", $"File copied and deleted: {files[i].FullName} -> {destinationPath}");
                        }
                    }
                    else
                    {
                        if (Test)
                        {
                            LogMessage("log.txt", $"File deleted (Test): {files[i].FullName}");
                        }
                        else
                        {
                            files[i].Delete();
                            LogMessage("log.txt", $"File deleted: {files[i].FullName}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage("log.txt", $"Failed to delete file: {files[i].FullName}. Error: {ex.Message}");
                }
            }
        }


        static void GenerateConfigFile(string rootFolderPath, int keepFileCount, string backupFolderPath = "")
        {
            var config = new MainConfig
            {
                RootPath = rootFolderPath,
                DefaultKeepFileCount = keepFileCount,
                DefaultBackupFolderPath = backupFolderPath,
                FolderList = new List<FolderConfig>()
            };

            var folders = Directory.GetDirectories(rootFolderPath, "*", SearchOption.AllDirectories);
            foreach (var folder in folders)
            {
                var bakFiles = Directory.GetFiles(folder, "*.bak");
                if (bakFiles.Length > 0)
                {
                    var newFolderPath = folder.Replace(rootFolderPath, "").Remove(0, 1);
                    config.FolderList.Add(new FolderConfig
                    {
                        FolderName = newFolderPath,
                        KeepFileCount = keepFileCount,
                        BackupFolderPath = null
                    });
                }
            }
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string configFilePath = Path.Combine(baseDirectory, "config.json");
            File.WriteAllText(configFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
            LogMessage("log.txt", $"Config file created at: {configFilePath}");
        }

        static void LogMessage(string logFilePath, string message)
        {
            File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
        }
    }
}