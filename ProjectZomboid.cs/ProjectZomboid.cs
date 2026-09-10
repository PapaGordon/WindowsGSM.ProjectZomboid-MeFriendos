using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Engine;
using WindowsGSM.GameServer.Query;

namespace WindowsGSM.Plugins
{
    // MeFriendos build based on WindowsGSM.ProjectZomboid by Richard Beard.
	public class ProjectZomboid : SteamCMDAgent // Project Zomboid is installed and updated through SteamCMD.
    {
        #region preparation of the WindowsAPI to send process shutdown signals
        internal const int CTRL_C_EVENT = 0;
        [DllImport("kernel32.dll")]
        internal static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool AttachConsole(uint dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        internal static extern bool FreeConsole();
        [DllImport("kernel32.dll")]
        static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);
        delegate Boolean ConsoleCtrlDelegate(uint CtrlType);
        #endregion

		// - Plugin details
        public Plugin Plugin = new Plugin
		{
			name = "WindowsGSM.ProjectZomboid",
			author = "MeFriendos",
			description = "WindowsGSM plugin for Project Zomboid Dedicated Server (MeFriendos build)",
			version = "0.1.1",
			url = "https://github.com/PapaGordon/WindowsGSM.ProjectZomboid-MeFriendos",
			color = "#38CDD4"
		};

		// - Standard Constructor and properties
		public ProjectZomboid(ServerConfig serverData) : base(serverData) => base.serverData = serverData;

		// - Settings properties for SteamCMD installer
		public override bool loginAnonymous => true; // Project Zomboid does not require a steam account to install the server, so loginAnonymous = true
		public override string AppId => "380870"; // Game server appId, Project Zomboid is 380870

		// - Game server Fixed variables
		public override string StartPath => "jre64\\bin\\java.exe"; // Bundled Java runtime used to start GameServer directly.
		public string FullName = "Project Zomboid Dedicated Server"; // Game server FullName
		public bool AllowsEmbedConsole = true;  // Does this server support output redirect?
		public int PortIncrements = 1; // This tells WindowsGSM how many ports should skip after installation
		public object QueryMethod = new A2S(); // Query method should be use on current server type. Accepted value: null or new A2S() or new FIVEM() or new UT3()

		// - Game server default values
		public string Port = "16261"; // Default port
		public string QueryPort = "16261"; // Default query port
		public string Defaultmap = "Muldraugh, KY"; // Default map name
		public string Maxplayers = "64"; // Default maxplayers
		public string Additional = ""; // Additional server start parameter

		// - Create a default cfg for the game server after installation
		public async void CreateServerCFG()
		{
		}

        private string GetParameters()
        {
            var param = new StringBuilder();
            DirectoryInfo homePath = new DirectoryInfo(ServerPath.GetServersServerFiles(serverData.ServerID));
            param.Append($"\"-Djava.awt.headless=true\" \"-Dzomboid.steam=1\" \"-Dzomboid.znetlog=1\" \"-Duser.home={homePath.Parent.FullName}\"");
            param.Append(" \"-XX:+UseZGC\" \"-XX:-CreateCoredumpOnCrash\" \"-XX:-OmitStackTraceInFastThrow\"");
            //if you have Memory issues you can try to edit -Xms16g -Xmx16g to better suite your system
            param.Append(" -Xms16g -Xmx16g \"-Djava.library.path=natives/;natives/win64/;.\" \"-Dstatistic=0\"");
            //java classpath copied from startScript
            param.Append(" -cp \"java/;java/projectzomboid.jar/\"");
            //actual start class
            param.Append(" zombie.network.GameServer");
            //add custom parameters and ports
            param.Append($" -port {serverData.ServerPort} {serverData.ServerParam} ");
            return param.ToString();
        }

        private bool RemoveAutomaticBroadFirewallRule()
        {
            string programPath = ServerPath.GetServersServerFiles(serverData.ServerID, StartPath);

            try
            {
                Type managerType = Type.GetTypeFromProgID("HNetCfg.FwMgr");
                if (managerType == null)
                    return false;

                object manager = Activator.CreateInstance(managerType);
                object localPolicy = manager.GetType().InvokeMember(
                    "LocalPolicy", BindingFlags.GetProperty, null, manager, null);
                object currentProfile = localPolicy.GetType().InvokeMember(
                    "CurrentProfile", BindingFlags.GetProperty, null, localPolicy, null);
                object applications = currentProfile.GetType().InvokeMember(
                    "AuthorizedApplications", BindingFlags.GetProperty, null, currentProfile, null);

                IEnumerable entries = applications as IEnumerable;
                if (entries == null)
                    return false;

                bool found = false;
                foreach (object application in entries)
                {
                    string applicationPath = Convert.ToString(application.GetType().InvokeMember(
                        "ProcessImageFileName", BindingFlags.GetProperty, null, application, null));

                    if (string.Equals(applicationPath, programPath, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return true;

                applications.GetType().InvokeMember(
                    "Remove", BindingFlags.InvokeMethod, null, applications, new object[] { programPath });

                applications = currentProfile.GetType().InvokeMember(
                    "AuthorizedApplications", BindingFlags.GetProperty, null, currentProfile, null);
                entries = applications as IEnumerable;
                if (entries == null)
                    return false;

                foreach (object application in entries)
                {
                    string applicationPath = Convert.ToString(application.GetType().InvokeMember(
                        "ProcessImageFileName", BindingFlags.GetProperty, null, application, null));

                    if (string.Equals(applicationPath, programPath, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

		// - Start server function, return its Process to WindowsGSM
		public async Task<Process> Start()
		{
			if (!RemoveAutomaticBroadFirewallRule())
			{
				Error = "Automatic firewall access could not be disabled. Start WindowsGSM as administrator or remove the broad Java rule manually.";
				return null;
			}

			// Prepare Process
            var p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = ServerPath.GetServersServerFiles(serverData.ServerID),
                    FileName = ServerPath.GetServersServerFiles(serverData.ServerID, StartPath),
                    Arguments = GetParameters(),
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false,
                },
                EnableRaisingEvents = true,
            };

            // Set up Redirect Input and Output to WindowsGSM Console if EmbedConsole is on
            if (serverData.EmbedConsole)
            {
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                var serverConsole = new ServerConsole(serverData.ServerID);
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
                Error = e.Message;
                return null; // return null if fail to start
            }
        }

        // - Stop server function
        public async Task Stop(Process p)
		{
			await Task.Run(() =>
			{
                if (!SendStopSignal(p))
                {
                    p.Kill();
                }
            });
		}

        //sends the stop signal to the process
        public static bool SendStopSignal(Process p)
        {
            if (AttachConsole((uint)p.Id))
            {
                SetConsoleCtrlHandler(null, true);
                try
                {
                    if (!GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0))
                    {
                        return false;
                    }
                    p.WaitForExit(10000);
                }
                finally
                {
                    SetConsoleCtrlHandler(null, false);
                    FreeConsole();
                }
                return true;
            }
            return false;
        }
    }
}
