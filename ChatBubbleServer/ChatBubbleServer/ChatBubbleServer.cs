using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Reflection;
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
                string output;
                
                output = "[" + currentTimeFull + "] " + NetComponents.ServerSetIPAddress(input); ;

                CommandLogger(output);
                return (output);
            }

            //Sets Server socket address
            public string SetSocket(string input)
            {
                string output;
                
                output = "[" + currentTimeFull + "] " + NetComponents.ServerSetSocket(input);

                CommandLogger(output);
                return (output);
            }

            //Autosets Server IP
            public string AutoIP()
            {
 
                string output;
                    
                output = "[" + currentTimeFull + "]" + " New Server IP address auto-set at " + NetComponents.ScanIP() + "\n";

                CommandLogger(output);
                return (output);
            }

            //Binds Server IP to socket
            public string Bind()
            {
                string output;

                output = "[" + currentTimeFull + "] " + NetComponents.ServerBind(NetComponents.ipAddress);

                CommandLogger(output);
                return (output);               
            }

            //Unbinds Server IP from socket
            public string Unbind()
            {
                string output;

                //State Availability Check
                if (ServerListening == true)
                {
                    CommandLogger("[" + currentTimeFull + "] Server unbind request failure.");
                    return ("[" + currentTimeFull + "] Can't unbind while listening. Use 'stoplisten' first.\n");
                }

                NetComponents.BreakBind(true);

                output = "[" + currentTimeFull + "]" + " Server has been unbound.\n";

                CommandLogger(output);
                return (output);             
            }

            //Starts listening for connections
            public string Listen()
            {
                string output;

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

                //Thread notificationManagerThread = new Thread();

                output = "[" + currentTimeFull + "] Started listening for connections on port " + NetComponents.socketAddress + "\n";

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
                string output = "ChatBubble Server Console v0.2\n\n";

                return (output);
            }

            //Clears the logfile
            public string ClearLog()
            {
                FileStream fileClearer = new FileStream(FileIOStreamer.defaultLogDirectory, FileMode.Truncate);
                fileClearer.Close();

                return ("[" + currentTimeFull + "] Log file '" + FileIOStreamer.defaultLogDirectory + "' cleared.\n");
            }

            public string Help()
            {
                //Returns the Help text

                string output = "Available commands:\n\nsetip [IPAddress] -- sets Server IPAddress\n\nsetsocket [socketaddress] -- sets Server socket address\n\n";
                output += "autoip -- auto IP detection (doesn't always work)\n\nbind -- bind current IP to current socket\n\nunbind -- unbinds current socket\n\n";
                output += "listen -- start port listening\n\nstoplisten -- stop port listening\n\nclear -- clear log window text\n\nclearlog -- clear log file\n\nhelp -- help\n\nshutdown - full Server stop\n";

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

                if (Environment.OSVersion.Platform == PlatformID.Unix) //Changes defaults for unix os
                {
                    FileIOStreamer.defaultRegisteredUsersDirectory = "/var/chat-bubble/users/regusers/";
                    FileIOStreamer.defaultActiveUsersDirectory = "/var/chat-bubble/active-sessions/";
                    FileIOStreamer.defaultPendingMessagesDirectory = "/var/chat-bubble/pending-messages/";
                }

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
                ConsoleCommands consoleCommand = new ConsoleCommands();

                string appendableText = "";
                mainCommandPart = "";                

                logTextbox.Text = logTextbox.Text + ">" + logCommandText;
                logTextbox.AppendText("\n");
                logTextbox.ScrollToCaret();

                for (int i = 0; i < logCommandText.Length; i++)
                {
                    if (logCommandText[i] != ' ' && i < logCommandText.Length)
                    {
                        mainCommandPart += logCommandText[i];
                    }
                    else
                    {
                        break;
                    }
                }             
                
                //Checking for specific command string to cast a command

                if (mainCommandPart == "help")
                {
                    appendableText = consoleCommand.Help();                   
                }
                if (mainCommandPart == "clear")
                {
                    logTextbox.Text = consoleCommand.Clear();
                }              
                if (mainCommandPart == "clearlog")
                {
                    appendableText = consoleCommand.ClearLog();
                }
                if (mainCommandPart == "setip")
                {
                    appendableText = consoleCommand.SetIP(logCommandText);
                }              
                if (mainCommandPart == "setsocket")
                {
                    appendableText = consoleCommand.SetSocket(logCommandText);
                }            
                if (mainCommandPart == "autoip")
                {
                    appendableText = consoleCommand.AutoIP();
                }               
                if (mainCommandPart == "unbind")
                {
                    appendableText = consoleCommand.Unbind();
                }            
                if (mainCommandPart == "bind")
                {
                    appendableText = consoleCommand.Bind();
                }               
                if (mainCommandPart == "listen")
                {
                    appendableText = consoleCommand.Listen();                    
                }               
                if (mainCommandPart == "shutdown")
                {
                    consoleCommand.Shutdown();
                }    
                if (mainCommandPart == "stoplisten")
                {
                    appendableText = consoleCommand.StopListen();
                }

                logTextbox.AppendText(appendableText);
               
                
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
