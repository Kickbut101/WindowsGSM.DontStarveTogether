using System;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Engine;
using WindowsGSM.GameServer.Query;
using System.IO;
using System.Linq;
using System.Net;


namespace WindowsGSM.Plugins
{
    public class DST : SteamCMDAgent // SteamCMDAgent is used because DST relies on SteamCMD for installation and update process
    {
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.DontStarveTogether", // WindowsGSM.XXXX
            author = "Andy",
            description = "🧩 WindowsGSM plugin for supporting Don't Starve Together Dedicated Server",
            version = "1.0",
            url = "https://github.com/Kickbut101/WindowsGSM.DontStarveTogether", // Github repository link (Best practice)
            color = "#800080"
        };


        // - Standard Constructor and properties
        public DST(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
        private readonly ServerConfig _serverData; // Store server start metadata, such as start ip, port, start param, etc


        // - Settings properties for SteamCMD installer
        public override bool loginAnonymous => true;
        public override string AppId => "343050"; // Game server appId, DST is 343050


        // - Game server Fixed variables
        public string directoryPath = @"\bin\";
        public override string StartPath => @"dontstarve_dedicated_server_nullrenderer.exe"; // Game server start path, for DST, it is \bin\dontstarve_dedicated_server_nullrenderer.exe
        public string FullName = "Don't Starve Together"; // Game server FullName
        public bool AllowsEmbedConsole = true;  // Does this server support output redirect?
        public int PortIncrements = 1; // This tells WindowsGSM how many ports should skip after installation
        public object QueryMethod = new A2S(); // Query method should be use on current server type. Accepted value: null or new A2S() or new FIVEM() or new UT3()


        // - Game server default values
        public string Port = "10999"; // Default port
        public string QueryPort = "27016"; // Default query port
        public string Defaultmap = "MyDediServer"; // Default map name
        public string Maxplayers = "20"; // Default maxplayers
        public string Additional = ""; // Additional server start parameter


        // - Create a default cfg for the game server after installation
        public async void CreateServerCFG() 
        { 
            var sb = new StringBuilder();
            sb.AppendLine($"motd={_serverData.ServerName}");
        }


        // - Start server function, return its Process to WindowsGSM
        public async Task<Process> Start()
        {
            string shipWorkingPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID);
            shipWorkingPath = shipWorkingPath + directoryPath;
            string shipExePath = shipWorkingPath + StartPath;

            if (!File.Exists(shipExePath))
            {
                Error = $"{Path.GetFileName(shipExePath)} not found ({shipExePath})";
                return null;
            }	

            // Prepare start parameter THESE NEED TO BE MODIFIED!!!!!!!!
            // MODIFY THESESESETESESEE
            var param = new StringBuilder();
            param.Append(string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $" -steam_master_server_port {_serverData.ServerPort}");
            param.Append(string.IsNullOrWhiteSpace(_serverData.ServerName) ? string.Empty : $" -name=\"{_serverData.ServerName}\"");
            param.Append(string.IsNullOrWhiteSpace(_serverData.ServerParam) ? string.Empty : $" {_serverData.ServerParam}"); 
 
            // Prepare Process
            var p = new Process
            {
                StartInfo =
                {
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false,
                    WorkingDirectory = shipWorkingPath,
                    FileName = shipExePath,
                    Arguments = ""
                },
                EnableRaisingEvents = true
            };


            // Set up Redirect Input and Output to WindowsGSM Console if EmbedConsole is on
            if (AllowsEmbedConsole)
            {
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                var serverConsole = new ServerConsole(_serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;

                // Start Process
                try
                {
                    p.Start();
                }
                catch (Exception e)
                {
                    Error = e.Message;
                    return null; // return null if fail to start
                }

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                return p;

            }
            // Start Process
            try
            {
                p.Start();
                return p;
            }
            catch (Exception e)
            {
                base.Error = e.Message;
                return null; // return null if fail to start
            }
        }


        // - Stop server function
        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                if (p.StartInfo.RedirectStandardInput)
                {
                    // Send "save and shutdown" command to StandardInput stream if EmbedConsole is on
                    p.StandardInput.WriteLine("c_shutdown(true)");
                }
                else
                {
                    // Send "save and shutdown" command to game server process MainWindow
                    ServerConsole.SendMessageToMainWindow(p.MainWindowHandle, "c_shutdown(true)");
                }
            });
        }
    }
}
