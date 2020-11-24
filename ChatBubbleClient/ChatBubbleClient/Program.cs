using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ChatBubble.Client
{
    static class Program
    {
        [DllImport("Shcore.dll")]
        static extern int SetProcessDpiAwareness(int PROCESS_DPI_AWARENESS);

        // According to https://msdn.microsoft.com/en-us/library/windows/desktop/dn280512(v=vs.85).aspx
        private enum DpiAwareness
        {
            None = 0,
            SystemAware = 1,
            PerMonitorAware = 2
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            SetMainDirectory();

            Application.SetCompatibleTextRenderingDefault(false);

            SetProcessDpiAwareness((int)DpiAwareness.PerMonitorAware);
            Application.Run(new Form1());          
        }

        static void SetMainDirectory()
        {
            Configuration configFile = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);

            string mainDirectory = configFile.AppSettings.Settings["executableDirectory"].Value;

            FileIOStreamer.SetClientRootDirectory(mainDirectory);
        }
    }
}
