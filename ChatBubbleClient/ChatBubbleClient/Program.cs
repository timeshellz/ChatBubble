using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.Reflection;

namespace ChatBubble.Client
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            SetMainDirectory();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
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
