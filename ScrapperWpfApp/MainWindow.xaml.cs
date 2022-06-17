using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scrapper.Models;
using Scrapper.Services;
using ScrapperBlazorLibrary.Data;

namespace ScrapperWpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CancellationTokenSource cts;
        private readonly IWatcherService _watcherService;
        private readonly IFileService _fileService;
        private readonly List<Task> _windowTasks;

        public MainWindow(IWatcherService watcherService, IFileService fileService)
        {
            cts = new();
            _windowTasks = new();
            _watcherService = watcherService;
            _fileService = fileService;

            Resources.Add("serviceCollection", App.ServiceProvider);

            List<string> lines = _fileService.ReadCsv("files/config", "streams.txt");

            if (!lines.Any())
            {
                //Not Implemented
            }
            _windowTasks.Add(_watcherService.StreamingWatcherAsync(lines, EnumsModel.ScrapperMode.Delayed, cts.Token));

            InitializeComponent();
        }

        private async void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            cts.Cancel();
            await Task.WhenAll(_windowTasks);
            _ = "Finished";
        }

    }
}