using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Reflection;
using System.Configuration;
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

        public static int newClientAmount = 0;
        public static int oldClientAmount = 0;
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
        public class ConsoleCommands
        {
            //List of all commands accepted by the Server console

            //Sets Server IP address
            public string SetIP(string input)
            {              
                string output = "[" + currentTimeFull + "] " + NetComponents.ServerSetIPAddress(input); ;

                CommandLogger(output);
                return (output);
            }

            //Sets Server socket address
            public string SetSocket(string input)
            {              
                string output = "[" + currentTimeFull + "] " + NetComponents.ServerSetSocket(input);

                CommandLogger(output);
                return (output);
            }

            //Autosets Server IP
            public string AutoIP()
            {  
                string output = "[" + currentTimeFull + "]" + " New Server IP address auto-set at " + NetComponents.ScanIP() + "\n";

                CommandLogger(output);
                return (output);
            }

            //Binds Server IP to socket
            public string Bind()
            {
                string output = "[" + currentTimeFull + "] " + NetComponents.ServerBind(NetComponents.ipAddress);

                CommandLogger(output);
                return (output);               
            }

            //Unbinds Server IP from socket
            public string Unbind()
            {
                //State Availability Check
                if (ServerListening == true)
                {
                    CommandLogger("[" + currentTimeFull + "] Server unbind request failure.");
                    return ("[" + currentTimeFull + "] Can't unbind while listening. Use 'stoplisten' first.\n");
                }

                NetComponents.BreakBind(true);

                string output = "[" + currentTimeFull + "]" + " Server has been unbound.\n";

                CommandLogger(output);
                return (output);             
            }

            //Starts listening for connections
            public string Listen()
            {
                if (NetComponents.ServerSocketBoundCheck() != "")
                {
                    return (NetComponents.ServerSocketBoundCheck());
                }

                //Start listening for handshakes
                Thread connectionScannerThread = new Thread(NetComponents.ServerListeningState);
                connectionScannerThread.Start();

                //Start checking for stale sessions
                Thread sessionCheckerThread = new Thread(NetComponents.SessionTimeOutCheck);
                sessionCheckerThread.Start();

                string output = "[" + currentTimeFull + "] Started listening for connections on port " + NetComponents.socketAddress + "\n";

                ServerListening = true;

                CommandLogger(output);
                return (output);
            }

            //Stops listening for connections
            public string StopListen()
            {              
                string output = "[" + currentTimeFull + "] " + NetComponents.ServerStopListen();

                ServerListening = false;
                NetComponents.BreakBind(true);
                NetComponents.ServerBind(NetComponents.ipAddress);

                CommandLogger(output);
                return (output);
            }

            //Clears the console
            //Returns the hat part of console text
            public string Clear()
            {
                return ("ChatBubble Server Console v" + assemblyVersion + "\n\n");
            }

            //Clears the logfile
            public string ClearLog()
            {
                FileIOStreamer fileIO = new FileIOStreamer();
                fileIO.ClearFile(FileIOStreamer.defaultLogDirectory);

                return ("[" + currentTimeFull + "] Log file '" + FileIOStreamer.defaultLogDirectory + "' cleared.\n");
            }

            public string SetDirectories(string path = "")
            {
                string[] directoryData = new string[4];

                Configuration configFile = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);

                directoryData[0] = configFile.AppSettings.Settings["databaseDirectory"].Value;
                directoryData[1] = configFile.AppSettings.Settings["usersFolder"].Value;
                directoryData[2] = configFile.AppSettings.Settings["sessionsFolder"].Value;
                directoryData[3] = configFile.AppSettings.Settings["pendingMessagesFolder"].Value;

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
                    if(Environment.OSVersion.Platform == PlatformID.Unix)
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
                    if(Environment.OSVersion.Platform == PlatformID.Unix)
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

                configFile.Save();
                ConfigurationManager.RefreshSection("appSettings");

                FileIOStreamer.SetServerDirectories(directoryData);

                return ("[" + currentTimeFull + "] Database directory set to \"" + directoryData[0] + "\"\n");
            }

            public string Help()
            {
                //Returns the Help text

                string output = "Available commands:\n\nsetip [IPAddress] -- sets Server IPAddress\n\nsetsocket [socketaddress] -- sets Server socket address\n\n" +
                    "autoip -- auto-detect local IP\n\nbind -- bind current IP to current socket\n\nunbind -- unbinds current socket\n\n" +
                    "listen -- start port listening\n\nstoplisten -- stop port listening\n\nclear -- clear log window text\n\nclearlog -- clear log file\n\n" +
                    "setdir [directory] -- set database directory\n\nhelp -- help\n\nshutdown - full Server stop\n";

                return (output);
            }

            //Server Shutdown
            public void Shutdown()
            {
                CommandLogger("\n[" + localDate.ToLongDateString() + ", " + localDate.ToLongTimeString() + "] Server session ended.\n\n\n");
                Environment.Exit(0);
            }

            //Session start (inaccesible command)
            public string ConsoleSessionStart()
            {
                CommandLogger("[" + localDate.ToLongDateString() + ", " + localDate.ToLongTimeString() + "] New Server session started.\n\n");

                SetDirectories();

                return ("ChatBubble Server Console v" + assemblyVersion + "\n\n");
            }

            //Command Logger
            public void CommandLogger(string input)
            {
                //FileIOStreamer fileIO = new FileIOStreamer();
                //fileIO.WriteToFile(FileIOStreamer.defaultLogDirectory, input, true);
            }
        }
        

        //Form------------------------------------Form------------------------------------Form------------------------------------Form
        public Server()
        {
            InitializeComponent();
            ActiveControl = commandTextbox;

            ConsoleCommands consoleInit = new ConsoleCommands();
            logTextbox.Text = consoleInit.ConsoleSessionStart();
            consoleInit.AutoIP();
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
                ConsoleCommands ConsoleCommands = new ConsoleCommands();

                string commandOutput = "";
                string commandArgument;

                logTextbox.Text = logTextbox.Text + ">" + logCommandText;
                logTextbox.AppendText("\n");
                logTextbox.ScrollToCaret();

                string[] commandSubstrings = logCommandText.Split(new char[] { ' ' });     
                //[0] - command, [1] - conditions

                if(commandSubstrings.Length > 1)
                {
                    commandArgument = commandSubstrings[1];
                }
                else
                {
                    commandArgument = "";
                }

                switch(commandSubstrings[0])
                {
                    case "help":
                        commandOutput = ConsoleCommands.Help();
                        break;
                    case "clear":
                        commandOutput = ConsoleCommands.Clear();
                        break;
                    case "clearlog":
                        commandOutput = ConsoleCommands.ClearLog();
                        break;
                    case "setip":
                        commandOutput = ConsoleCommands.SetIP(commandArgument);
                        break;
                    case "setsocket":
                        commandOutput = ConsoleCommands.SetSocket(commandArgument);
                        break;
                    case "autoip":
                        commandOutput = ConsoleCommands.AutoIP();
                        break;
                    case "unbind":
                        commandOutput = ConsoleCommands.Unbind();
                        break;
                    case "bind":
                        commandOutput = ConsoleCommands.Bind();
                        break;
                    case "listen":
                        commandOutput = ConsoleCommands.Listen();
                        break;
                    case "shutdown":
                        ConsoleCommands.Shutdown();
                        break;
                    case "stoplisten":
                        commandOutput = ConsoleCommands.StopListen();
                        break;
                    case "setdir":
                        commandOutput = ConsoleCommands.SetDirectories(commandArgument);
                        break;
                    default:
                        commandOutput = "[" + currentTimeFull + "] Unknown command.\n";
                        break;
                }

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
            ConsoleCommands consoleEmergencyLog = new ConsoleCommands();

            consoleEmergencyLog.CommandLogger("\n[" + localDate.ToLongDateString() + ", " + localDate.ToLongTimeString() + "] Server session ended abruptly.\n\n\n");

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
