

using System.Reflection;

string[] x = Environment.GetCommandLineArgs();
string config = Directory.GetCurrentDirectory() + "/config.txt";
bool first = false;

if(x.Length > 1)
{
    config = x[1];
}

if (!File.Exists(config))
{
    string config_directory = Path.GetDirectoryName(config);

    if (!Directory.Exists(config_directory))
    {
        Directory.CreateDirectory(config_directory);
    }

    var assembly = Assembly.GetExecutingAssembly();
    var resourceName = "HSmartBackupClient.SystemText.config.txt";
    Stream stream = assembly.GetManifestResourceStream(resourceName);
    StreamReader reader = new StreamReader(stream);
    string result = reader.ReadToEnd();

    File.WriteAllText(config, result);

    first = true;
}

if (first)
{
    Console.WriteLine("Config file has been created. Please make sure you put a correct setting, then can rerun this command.");
}
else
{
    List<string> paths = new List<string>();
    List<string> servers = new List<string>();
    string mode = "";
    string aes_key = "";
    string timer = "";

    List<string> crs = File.ReadAllLines(config).ToList();

    foreach (string cr in crs)
    {
        if (!string.IsNullOrEmpty(cr))
        {
            if (cr[0].ToString() != "#")
            {
                string[] kv = cr.Split('=');
                string key = kv[0];
                string value = kv[1];

                if (!string.IsNullOrEmpty(value))
                {
                    switch (key)
                    {
                        case "mode":
                            mode = value;
                            break;

                        case "path":
                            if (!paths.Contains(value))
                            {
                                paths.Add(value);
                            }
                            break;

                        case "server":
                            if (!servers.Contains(value))
                            {
                                servers.Add(value);
                            }
                            break;

                        case "aes_key":
                            aes_key = value;
                            break;
                    }
                }
            }
        }
    }

    if (!string.IsNullOrEmpty(mode))
    {
        string[] ms = mode.Split(':');

        if(ms.Length == 1 && ms[0] == "realtime")
        {
            mode = "realtime";
        }
        else if(ms.Length == 2)
        {
            mode = ms[0];
            timer = ms[1];
        }
        else
        {
            mode = "";
        }
    }

    foreach (string path in paths)
    {
        Task.Run(() => Watching(path, mode));
    }

    Console.ReadLine();
}

async void Watching(string path, string mode)
{
    var watcher = new FileSystemWatcher(path);

    watcher.EnableRaisingEvents = true;
    watcher.NotifyFilter = NotifyFilters.CreationTime
                        | NotifyFilters.DirectoryName
                        | NotifyFilters.FileName
                        | NotifyFilters.LastWrite;

    watcher.IncludeSubdirectories = true;

    watcher.Changed += Watcher_Changed;
    watcher.Created += Watcher_Created;
    watcher.Renamed += Watcher_Renamed;
    watcher.Deleted += Watcher_Deleted;

    void Watcher_Deleted(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine("Delete: " + e.FullPath);
    }

    void Watcher_Renamed(object sender, RenamedEventArgs e)
    {
        Console.WriteLine("Rename: " + e.OldFullPath + " to " + e.FullPath);
    }

    void Watcher_Created(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine("Create: " + e.FullPath);
    }

    void Watcher_Changed(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine("Changed: " + e.FullPath);
    }
}