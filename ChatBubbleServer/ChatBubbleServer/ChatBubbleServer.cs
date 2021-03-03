using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Reflection;
using System.Configuration;
using System.Runtime.InteropServices;
using ChatBubble;

using ChatBubble.ServerAPI;
using ChatBubble.SharedAPI;

namespace ChatBubble.Server
{
    public partial class Server : Form
    {
        public static ServerNetworkConfigurator NetworkConfigurator { get; private set; }

        //Server State;
        public static bool ServerListening { get; set; }
        public static bool ServerBound { get; set; }

        //Assembly Version
        public static string AssemblyVersion = typeof(Server).Assembly.GetName().Version.Major + "." + typeof(Server).Assembly.GetName().Version.Minor;

        //Client_State_Scanner
        static int alstatSocketAddress = 8001;

        static int actualClientAmount = 0;
        static int actualLoggedInAmount = 0;

        //Log vars
        static string logCommandText = "";
        static string mainCommandPart = "";

        //Timekeeping
        static DateTime LocalDate { get; set; }
        static string currentTimeSeconds;
        static string currentTimeFull;
        static string currentDateFull;

        public static DateTime ServerSessionStartTime;
        public static DateTime ServerListeningStartTime;
        

        //Form------------------------------------Form------------------------------------Form------------------------------------Form
        public Server()
        {
            InitializeComponent();

            FileIOStreamer.LoggingEnabled = true;

            AutoScaleMode = AutoScaleMode.Font;
            ActiveControl = commandTextbox;

            NetworkConfigurator = new ServerNetworkConfigurator();

            ConsoleCommands.ExecuteCommand(ConsoleCommands.CommandType.startsession, false, out string output);

            logTextbox.AppendText(output);

            ConsoleCommands.ExecuteCommand(ConsoleCommands.CommandType.autoip, false,  out output);

            logTextbox.AppendText(output);

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(ManageUnhandledException);
        }

        private void ManageUnhandledException(object sender, UnhandledExceptionEventArgs eventArgs)
        {
            try
            {
                Exception exception = (Exception)eventArgs.ExceptionObject;

                string exceptionDescriptionString = exception.GetType().ToString() + " at " + exception.Source.ToString() + ". " + exception.Message;

                FileIOStreamer.LogWriter("Unhandled exception of type " + exceptionDescriptionString + " occured.");
                FileIOStreamer.LogWriter("Server shutting down.");
            }
            catch
            {
                Application.Exit();
            }
        }

        private void command_textbox_TextChanged(object sender, EventArgs e)
        {
            logCommandText = commandTextbox.Text;        
        }

        //Command input
        private void command_textbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode.Equals(Keys.Enter) && logCommandText != "")
            {
                ConsoleCommands.CommandType commandType;

                string commandOutput;

                logTextbox.Text = logTextbox.Text + ">" + logCommandText;
                logTextbox.AppendText("\n");
                logTextbox.ScrollToCaret();

                string[] commandSubstrings = logCommandText.Split(new char[] { ' ' });
                //[0] - command, [1]+ - conditions

                try
                {
                    commandType = (ConsoleCommands.CommandType)Enum.Parse(typeof(ConsoleCommands.CommandType), commandSubstrings[0], false);

                    if (commandType == ConsoleCommands.CommandType.startsession) commandType = ConsoleCommands.CommandType.None;
                    if (commandType == ConsoleCommands.CommandType.clear) logTextbox.Clear();
                }
                catch
                {
                    commandType = ConsoleCommands.CommandType.None;
                }

                string[] argumentSubstrings = new string[commandSubstrings.Length - 1];

                for(int i = 1; i < commandSubstrings.Length; i++)
                {
                    argumentSubstrings[i - 1] = commandSubstrings[i];
                }
                              
                ConsoleCommands.ExecuteCommand(commandType, true, out commandOutput, argumentSubstrings);
                logTextbox.AppendText(commandOutput);

                commandTextbox.Text = "";
                logCommandText = "";
            }
        }

        private void update_data_timer_Tick(object sender, EventArgs e)
        {
            if(ServerBound)
            {
                currentState1Textbox.Text = "Server bound";
            }
            else
            {
                currentState1Textbox.Text = "Server not bound";
            }

            if(ServerListening)
            {
                currentState2Textbox.Text = "Server listening";
            }
            else
            {
                currentState2Textbox.Text = "Server not listening";
            }

            currentIPTextbox.Text = "IP Address: " + ServerNetworkConfiguration.LocalAddress;
            currentSocketTextbox.Text = "TCP Socket: " + Convert.ToString(ServerNetworkConfiguration.LocalTCPPortNumber);
            currentTimeTextbox.Text = currentTimeFull;
            currentDateTextbox.Text = currentDateFull;

            LocalDate = DateTime.Now;

            currentTimeSeconds = LocalDate.Second.ToString();
            currentTimeFull = LocalDate.ToLongTimeString();
            currentDateFull = LocalDate.ToLongDateString();

           if (ServerListening)
            {
                clientCountTextbox.Visible = true;
                clientCountTextbox.Text = "Clients connected: " + actualClientAmount.ToString();

                loggedInCountTextbox.Visible = true;
                loggedInCountTextbox.Text = "Clients logged in: " + actualLoggedInAmount.ToString();
                clientStateRereshTimer.Start();             
            }
            else
            {
                clientCountTextbox.Visible = false;
                loggedInCountTextbox.Visible = false;

                clientStateRereshTimer.Stop();             
            }
            
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Emergency shutdown procedure

            //Attempting to stop all threads; logging shutdown
            FileIOStreamer.LogWriter("Server session ended abruptly.");

            ServerListening = false;

            Server.NetworkConfigurator.DisconnectSockets(false);
            Environment.Exit(0);
        }

        private void ClientStateReresh_Tick(object sender, EventArgs e)
        {
            actualClientAmount = ServerRequestManager.connectedClientEndpoints.Count;

            actualLoggedInAmount = ServerRequestManager.loggedInUserEndpoints.Count;           
        }
    }

    public static class ConsoleCommands
    {
        public enum CommandType
        {
            setip, setsocket, autoip, init, bind, unbind, listen, stoplisten,
            clear, clearlog, setdir, help, getconcodes, getserverstats, shutdown, startsession, None
        }

        public static void ExecuteCommand(CommandType commandType, bool requiresInputLogging, out string visibleCommandResult, string[] commandArguments = null)
        {
            string commandResult = "";
            visibleCommandResult = "";

            try
            {
                switch (commandType)
                {
                    case CommandType.help:
                        Help(out visibleCommandResult);
                        commandResult = "Help requested.";
                        break;
                    case CommandType.clear:
                        Clear(out visibleCommandResult);
                        commandResult = "Console cleared.";
                        break;
                    case CommandType.clearlog:
                        ClearLog(out commandResult);
                        break;
                    case CommandType.setip:
                        SetIP(commandArguments[0]);
                        commandResult = "Server IP set to " + commandArguments[0] + ".";
                        break;
                    case CommandType.autoip:
                        AutoIP();
                        commandResult = "New Server IP Endpoint auto-set at " + ServerNetworkConfiguration.LocalTCPIPEndPoint.ToString();
                        break;
                    case CommandType.unbind:
                        Unbind();
                        commandResult = "Sockets successfully disconnected.";
                        break;
                    case CommandType.init:
                        Initialize();
                        commandResult = "Sockets successfully initialized.";
                        break;
                    case CommandType.bind:
                        Bind();
                        commandResult = "Sockets successfully bound.";
                        break;
                    case CommandType.listen:
                        Listen();
                        commandResult = "Server started listening.";
                        break;
                    case CommandType.shutdown:
                        FileIOStreamer.LogWriter("Shutting down...");
                        ConsoleCommands.Shutdown();
                        break;
                    case CommandType.stoplisten:
                        StopListen();
                        commandResult = "Server stopped listening.";
                        break;
                    case CommandType.setdir:
                        SetDirectories(out commandResult, commandArguments[0]);
                        break;
                    case CommandType.getconcodes:
                        ListConCodes(out visibleCommandResult);
                        commandResult = "Connection code list requested.";
                        break;
                    case CommandType.getserverstats:
                        visibleCommandResult = ServerRequestManager.GetServerSessionStats();
                        commandResult = "Server statistics requested.";
                        break;
                    case CommandType.startsession:
                        ConsoleSessionStart(out visibleCommandResult);
                        commandResult = "New server session started.";
                        requiresInputLogging = false;
                        break;
                    case CommandType.None:
                        commandResult = "Unknown command.";
                        visibleCommandResult = "Unknown command requested.";
                        requiresInputLogging = false;
                        break;
                }
            }
            catch (Exception e)
            {
                commandResult = e.Message;
            }

            if (visibleCommandResult == "")
            {
                visibleCommandResult = commandResult;
            }
            visibleCommandResult += "\n";

            string logOutput = "Executed command \"" + commandType.ToString() + "\" with ";

            if (commandArguments != null && commandArguments.Length > 0)
            {
                logOutput += "arguments: ";

                for (int i = 0; i < commandArguments.Length; i++)
                {
                    logOutput += commandArguments[i] + " ";
                }
            }
            else
            {
                logOutput += "no aguments.";
            }

            if (requiresInputLogging)
            {
                FileIOStreamer.LogWriter(logOutput);
            }

            FileIOStreamer.LogWriter(commandResult);
        }


        //List of all commands accepted by the Server console

        //Sets Server IP address
        public static void SetIP(string input)
        {
            Server.NetworkConfigurator.SetLocalEndpoints(input);
        }

        //Autosets Server IP
        public static void AutoIP()
        {
            Configuration configFile = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);
            string autoSetOption = configFile.AppSettings.Settings["autoDetectIP"].Value;

            if (autoSetOption == "true" && configFile.AppSettings.Settings["defaultAddress"].Value != "")
            {
                string currentIP = configFile.AppSettings.Settings["defaultAddress"].Value;
                Server.NetworkConfigurator.SetLocalEndpoints(currentIP);               
            }
            else
            {
                Server.NetworkConfigurator.SetLocalEndpoints();
            }

            Server.NetworkConfigurator.SetPorts(8000);
        }

        public static void Initialize()
        {
            Server.NetworkConfigurator.InitializeSockets();
        }

        //Binds Server IP to socket
        public static void Bind()
        {
            Server.NetworkConfigurator.BindSockets();
            Server.ServerBound = true;
        }

        //Unbinds Server IP from socket
        public static void Unbind()
        {
            Server.NetworkConfigurator.DisconnectSockets(false);
            Server.ServerBound = false;
        }

        //Starts listening for connections
        public static void Listen()
        {
            //Start listening for handshakes
            Thread connectionScannerThread = new Thread(ServerRequestManager.ServerListeningState);
            connectionScannerThread.Start();

            //Start checking for stale sessions
            Thread sessionCheckerThread = new Thread(ServerRequestManager.SessionTimeOutCheck);
            sessionCheckerThread.Start();

            //Start updating runtime server stats
            Thread statUpdaterThread = new Thread(ServerRequestManager.ServerStatUpdater);
            statUpdaterThread.Start();

            Server.ServerListening = true;
        }

        //Stops listening for connections
        public static void StopListen()
        {
            Server.ServerListening = false;
            Server.NetworkConfigurator.DisconnectSockets(true);
        }

        //Clears the console
        //Returns the hat part of console text
        public static void Clear(out string output)
        {
            output = "ChatBubble Server Console v" + Server.AssemblyVersion + "\n\n";
        }

        //Clears the logfile
        public static void ClearLog(out string output)
        {
            FileIOStreamer.ClearFile(FileIOStreamer.defaultLogDirectory);

            output = "Log file '" + FileIOStreamer.defaultLogDirectory + "' cleared.";
        }

        public static void SetDirectories(out string output, string path = "")
        {
            string[] directoryData = new string[5];

            Configuration configFile = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);

            directoryData[0] = configFile.AppSettings.Settings["databaseDirectory"].Value;
            directoryData[1] = configFile.AppSettings.Settings["usersFolder"].Value;
            directoryData[2] = configFile.AppSettings.Settings["sessionsFolder"].Value;
            directoryData[3] = configFile.AppSettings.Settings["pendingMessagesFolder"].Value;
            directoryData[4] = @"\ChatBubbleLog.txt";

            for (int i = 0; i < directoryData.Length; i++)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && directoryData[i].Contains("\\"))
                {
                    directoryData[i] = "";
                }
            }

            if (directoryData[0] == "")
            {
                directoryData[0] = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
            else if (path != "")
            {
                directoryData[0] = path;
            }

            configFile.AppSettings.Settings["databaseDirectory"].Value = directoryData[0];

            if (directoryData[1] == "")
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    directoryData[1] = "/regusers/";
                }
                else
                {
                    directoryData[1] = @"\Users\";
                }

                configFile.AppSettings.Settings["usersFolder"].Value = directoryData[1];
            }

            if (directoryData[2] == "")
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    directoryData[2] = "/active-sessions/";
                }
                else
                {
                    directoryData[2] = @"\Active Sessions\";
                }

                configFile.AppSettings.Settings["sessionsFolder"].Value = directoryData[2];
            }

            if (directoryData[3] == "")
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    directoryData[3] = "/pending-messages/";
                }
                else
                {
                    directoryData[3] = @"\Pending Messages\";
                }

                configFile.AppSettings.Settings["pendingMessagesFolder"].Value = directoryData[3];
            }

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                directoryData[4] = "/chatbubble.log";
            }


            configFile.Save();
            ConfigurationManager.RefreshSection("appSettings");

            FileIOStreamer.SetServerDirectories(directoryData);

            output = "Database directory set to \"" + directoryData[0] + "\"";
        }

        public static void Help(out string output)
        {
            //Returns the Help text

            output = "Available commands:\n\nsetip [IPAddress] -- sets Server IPAddress\n\nsetsocket [socketaddress] -- sets Server socket address\n\n" +
                "autoip -- auto-detect local IP\n\nbind -- bind current IP to current socket\n\nunbind -- unbinds current socket\n\n" +
                "listen -- start port listening\n\nstoplisten -- stop port listening\n\nclear -- clear log window text\n\nclearlog -- clear log file\n\n" +
                "setdir [directory] -- set database directory\n\nhelp -- help\n\ngetconcodes -- list currently used connection codes\n\n" +
                "\n\ngetserverstats -- get current server statistics\n\nshutdown - full Server stop\n";
        }

        //Returns error codes
        public static void ListConCodes(out string output)
        {
            output = ConnectionCodes.GetAllErrorCodes();
        }

        //Server Shutdown
        public static void Shutdown()
        {
            FileIOStreamer.LogWriter("Server session ended.");
            Environment.Exit(0);
        }

        //Session start (inaccesible command)
        public static void ConsoleSessionStart(out string output)
        {
            SetDirectories(out output);
            FileIOStreamer.LogWriter(output);

            Server.ServerSessionStartTime = DateTime.Now;

            string currentPlatform = "";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                currentPlatform = " - Linux";
            }

            Server.NetworkConfigurator.SetPorts(9000);
            Server.NetworkConfigurator.SetLocalEndpoints("25.100.142.27");
            Server.NetworkConfigurator.InitializeSockets();
            Server.NetworkConfigurator.BindSockets();

            output = "ChatBubble Server Console v" + Server.AssemblyVersion + currentPlatform + "\n\n";
        }
    }
}
