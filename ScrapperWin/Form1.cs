using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using ScrapperBlazorLibrary;
using ScrapperBlazorLibrary.Data;

namespace ScrapperWin
{
    public partial class Form1 : Form
    {
        public static readonly AppData _appState = new();
        public Form1()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddBlazorWebView();
            serviceCollection.AddWindowsFormsBlazorWebView();
            serviceCollection.AddSingleton<AppData>(_appState);
            serviceCollection.AddSingleton<WeatherForecastService>();

            InitializeComponent();

            blazorWebView1.HostPage = @"wwwroot\index.html";
            blazorWebView1.Services = serviceCollection.BuildServiceProvider();
            blazorWebView1.RootComponents.Add<App>("#app");
        }
    }
}