using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ChatBubble;
using System.Reflection;

using ChatBubble.SharedAPI;
using ChatBubble.ClientAPI;

namespace ChatBubbleClientWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MapViews();
            ManageApplicationConfiguration();

            ViewModels.LoadingWindowViewModel viewModel = new ViewModels.LoadingWindowViewModel(new Utility.WindowFactory());
        }

        private void ManageApplicationConfiguration()
        {
            Configuration configFile = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);

           // SetFilesystemConfiguration(configFile);
            SetNetworkConfiguration(configFile);
        }

        private void SetFilesystemConfiguration(Configuration configFile)
        {
            string mainDirectory = configFile.AppSettings.Settings["executableDirectory"].Value;
        }

        private void SetNetworkConfiguration(Configuration configFile)
        {
            string[] serverAddress = configFile.AppSettings.Settings["serverAddress"].Value.Split(':');

            ClientNetworkConfigurator configurator = new ClientNetworkConfigurator();

            configurator.InitializeSockets();
            configurator.SetServerEndPoint(serverAddress[0], Convert.ToInt32(serverAddress[1]));
        }

        private void MapViews()
        {
            Utility.ViewModelResolver resolver = new Utility.ViewModelResolver();

            resolver.MapNewView(typeof(ViewModels.LoadingWindowViewModel), typeof(LoadingWindow));
            resolver.MapNewView(typeof(ViewModels.LoginWindowViewModel), typeof(LoginWindow));
            resolver.MapNewView(typeof(ViewModels.MainWindowViewModel), typeof(MainWindow));
        }
    }
}
