using System;
using System.Collections.Generic;
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

        public MainWindow(IWatcherService watcherService, IFileService fileService)
        {
            cts = new();
            _watcherService = watcherService;
            _fileService = fileService;

            Resources.Add("serviceCollection", App.ServiceProvider);

            try
            {
                List<string> lines = _fileService.ReadCsv("files/config", "streams.txt");

                if (!lines.Any())
                {
                    //Not Implemented
                }
                _watcherService.StreamingWatcherAsync(lines, EnumsModel.ScrapperMode.Delayed, cts.Token);
                
                InitializeComponent();
            }
            catch (Exception)
            {
                //Not Implemented
                _ = "";
            }
        }
    }
}