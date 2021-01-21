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

namespace ChatBubble.Server
{
    public partial class Server : Form
    {
        //Server State;
        public static bool ServerListening;

        //Assembly Version
        public static string assemblyVersion = typeof(Server).Assembly.GetName().Version.Major + "." + typeof(Server).Assembly.GetName().Version.Minor;

        //Client_State_Scanner
        public static int alstatSocketAddress = 8001;

        public static int actualClientAmount = 0;
        public static int actualLoggedInAmount = 0;

        //Universal_Encoding
        public static Encoding us_US = Encoding.GetEncoding(20127);

        //Log vars
        public static string logCommandText = "";
        public static string mainCommandPart = "";

        //Timekeeping
        public static DateTime localDate = DateTime.Now;
        public static string currentTimeSeconds;
        public static string currentTimeFull;
        public static string currentDateFull;

        //Server Console Commands-----------------------Server Console Commands---------------------------Server Console Commands-----------------------------Server Console Commands
        public static class ConsoleCommands
        {
            public enum CommandType { setip, setsocket, autoip, bind, unbind, listen, stoplisten,
                clear, clearlog, setdir, help, getconcodes, getserverstats, shutdown, startsession, None }

            public static void ExecuteCommand(CommandType commandType, bool requiresInputLogging, out string visibleCommandResult, string[] commandArguments = null)
            {
                string commandResult = "";
                visibleCommandResult = "";

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
                        SetIP(commandArguments[0], out commandResult);
                        break;
                    case CommandType.setsocket:
                        SetSocket(commandArguments[0], out commandResult);
                        break;
                    case CommandType.autoip:
                        AutoIP(out commandResult);
                        break;
                    case CommandType.unbind:
                        Unbind(out commandResult);
                        break;
                    case CommandType.bind:
                        Bind(out commandResult);
                        break;
                    case CommandType.listen:
                        Listen(out commandResult);
                        break;
                    case CommandType.shutdown:
                        FileManager.LogWriter("Shutting down...");
                        ConsoleCommands.Shutdown();
                        break;
                    case CommandType.stoplisten:
                        StopListen(out commandResult);
                        break;
                    case CommandType.setdir:
                        SetDirectories(out commandResult, commandArguments[0]);
                        break;
                    case CommandType.getconcodes:
                        ListConCodes(out visibleCommandResult);
                        commandResult = "Connection code list requested.";
                        break;
                    case CommandType.getserverstats:
                        visibleCommandResult = NetComponents.GetServerSessionStats();
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

                if(visibleCommandResult == "")
                {
                    visibleCommandResult = commandResult + "\n";
                }

                string logOutput = "Executed command \"" + commandType.ToString() + "\" with ";

                if(commandArguments != null && commandArguments.Length > 0)
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
                    FileManager.LogWriter(logOutput);
                }

                FileManager.LogWriter(commandResult);
            }


            //List of all commands accepted by the Server console

            //Sets Server IP address
            public static void SetIP(string input, out string output)
            {              
                output = NetComponents.ServerSetIPAddress(input); ;
            }

            //Sets Server socket address
            public static void SetSocket(string input, out string output)
            {              
                output = NetComponents.ServerSetSocket(input);
            }

            //Autosets Server IP
            public static void AutoIP(out string output)
            {
                Configuration configFile = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);
                string autoSetOption = configFile.AppSettings.Settings["autoDetectIP"].Value;
                string currentIP;

                if(autoSetOption == "true" && configFile.AppSettings.Settings["defaultAddress"].Value != "")
                {
                    currentIP = configFile.AppSettings.Settings["defaultAddress"].Value;
                    NetComponents.ServerSetIPAddress(currentIP);
                }
                else
                {
                    currentIP = NetComponents.ScanIP();
                }

                output = "New Server IP address auto-set at " + currentIP;
            }

            //Binds Server IP to socket
            public static void Bind(out string output)
            {
                output = NetComponents.ServerBind(NetComponents.ipAddress);               
            }

            //Unbinds Server IP from socket
            public static void Unbind(out string output)
            {
                //State Availability Check
                if (ServerListening == true)
                {
                    output = "Can't unbind while listening. Use 'stoplisten' first.";
                }

                NetComponents.BreakBind(true);

                output = "Server has been unbound.";             
            }

            //Starts listening for connections
            public static void Listen(out string output)
            {
                if (NetComponents.ServerSocketBoundCheck() != "")
                {
                    output = NetComponents.ServerSocketBoundCheck();
                }

                //Start listening for handshakes
                Thread connectionScannerThread = new Thread(NetComponents.ServerListeningState);
                connectionScannerThread.Start();

                //Start checking for stale sessions
                Thread sessionCheckerThread = new Thread(NetComponents.SessionTimeOutCheck);
                sessionCheckerThread.Start();

                //Start updating runtime server stats
                Thread statUpdaterThread = new Thread(NetComponents.ServerStatUpdater);
                statUpdaterThread.Start();

                output = "Started listening for connections on port " + NetComponents.socketAddress;

                ServerListening = true;
            }

            //Stops listening for connections
            public static void StopListen(out string output)
            {
                output = NetComponents.ServerStopListen();

                ServerListening = false;
                NetComponents.BreakBind(true);
                NetComponents.ServerBind(NetComponents.ipAddress);
            }

            //Clears the console
            //Returns the hat part of console text
            public static void Clear(out string output)
            {
                output = "ChatBubble Server Console v" + assemblyVersion + "\n\n";
            }

            //Clears the logfile
            public static void ClearLog(out string output)
            {
                FileManager.ClearFile(FileManager.defaultLogDirectory);

                output = "Log file '" + FileManager.defaultLogDirectory + "' cleared.";
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

                for(int i = 0; i < directoryData.Length; i++)
                {
                    if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && directoryData[i].Contains("\\"))
                    {
                        directoryData[i] = "";
                    }
                }

                if (directoryData[0] == "")
                {
                    directoryData[0] = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                }
                else if(path != "")
                {
                    directoryData[0] = path;
                }

                configFile.AppSettings.Settings["databaseDirectory"].Value = directoryData[0];

                if (directoryData[1] == "")
                {
                    if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        directoryData[1] = "/regusers/";
                    }
                    else
                    {
                        directoryData[1] = @"\Users\";
                    }

                    configFile.AppSettings.Settings["usersFolder"].Value = directoryData[1];
                }

                if(directoryData[2] == "")
                {
                    if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        directoryData[2] = "/active-sessions/";
                    }
                    else
                    {
                        directoryData[2] = @"\Active Sessions\";
                    }

                    configFile.AppSettings.Settings["sessionsFolder"].Value = directoryData[2];
                }

                if(directoryData[3] == "")
                {
                    if(Environment.OSVersion.Platform == PlatformID.Unix)
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

                FileManager.SetServerDirectories(directoryData);

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
                output = NetComponents.ConnectionCodes.GetAllErrorCodes();
            }

            //Server Shutdown
            public static void Shutdown()
            {
                FileManager.LogWriter("Server session ended.");
                Environment.Exit(0);
            }

            //Session start (inaccesible command)
            public static void ConsoleSessionStart(out string output)
            {
                SetDirectories(out output);
                FileManager.LogWriter(output);

                NetComponents.serverSessionStartTime = DateTime.Now;

                string currentPlatform = "";

                if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    currentPlatform = " - Linux";
                }

                output = "ChatBubble Server Console v" + assemblyVersion + currentPlatform + "\n\n";
            }
        }
        

        //Form------------------------------------Form------------------------------------Form------------------------------------Form
        public Server()
        {
            InitializeComponent();

            FileManager.LoggingEnabled = true;

            AutoScaleMode = AutoScaleMode.Font;
            ActiveControl = commandTextbox;

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

                FileManager.LogWriter("Unhandled exception of type " + exceptionDescriptionString + " occured.");
                FileManager.LogWriter("Server shutting down.");
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
            if(NetComponents.ServerSocketBoundCheck() == "")
            {
                currentState1Textbox.Text = "Server bound";
            }
            else
            {
                currentState1Textbox.Text = "Server not bound";
            }

            if(ServerListening == true)
            {
                currentState2Textbox.Text = "Server listening";
            }
            else
            {
                currentState2Textbox.Text = "Server not listening";
            }

            currentIPTextbox.Text = "IP Address: " + NetComponents.ipAddress;
            currentSocketTextbox.Text = "Socket Address: " + Convert.ToString(NetComponents.socketAddress);
            currentTimeTextbox.Text = currentTimeFull;
            currentDateTextbox.Text = currentDateFull;

            localDate = DateTime.Now;

            currentTimeSeconds = localDate.Second.ToString();
            currentTimeFull = localDate.ToLongTimeString();
            currentDateFull = localDate.ToLongDateString();

           if (ServerListening == true)
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
            FileManager.LogWriter("Server session ended abruptly.");

            ServerListening = false;

            NetComponents.ServerStopListen();
            NetComponents.BreakBind(false);
            Environment.Exit(0);

        }

        private void ClientStateReresh_Tick(object sender, EventArgs e)
        {
            actualClientAmount = NetComponents.connectedClientsBlockingCollection.Count;

            actualLoggedInAmount = NetComponents.loggedInUsersBlockingCollection.Count;           
        }
    }
}
