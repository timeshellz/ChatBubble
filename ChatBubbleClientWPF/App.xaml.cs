using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ChatBubble;
using System.Reflection;

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
            SetConfiguration();

            ViewModels.LoadingWindowViewModel viewModel = new ViewModels.LoadingWindowViewModel(new Utility.WindowFactory());
        }

        private void SetConfiguration()
        {
            Configuration configFile = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);

            string mainDirectory = configFile.AppSettings.Settings["executableDirectory"].Value;

            FileIOStreamer.SetClientRootDirectory(mainDirectory);
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
