using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using System.Xml;

namespace QualiTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            LoadLog4NetConfig();

            Application.Current.DispatcherUnhandledException += ViewModel.GlobalExceptionHandler;

            this.InitializeComponent();
        }

        private static void LoadLog4NetConfig()
        {
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config"));

            var repo = log4net.LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));

            log4net.Config.XmlConfigurator.Configure(repo, log4netConfig["log4net"]);
        }
    }
}