using LivesteamScrapper.Models;
using Microsoft.AspNetCore.Mvc;

namespace LivesteamScrapper.Controllers
{
    public class StreamingController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private List<Stream> streams;

        public StreamingController(ILogger<HomeController> logger)
        {
            _logger = logger;
            streams = new List<Stream>();
        }

        public void AddStream(string website, string channelPath)
        {
            if(streams.FindIndex(stream => stream.Website == website && stream.Channel == channelPath) < 0)
            {
                EnvironmentModel environment = EnvironmentModel.GetEnvironment(website);
                ScrapperController scrapperController = new ScrapperController(_logger, environment, channelPath);
                Stream stream = new(website, channelPath, environment, scrapperController);
                streams.Add(stream);
            }
        }
    }

    internal class Stream
    {
        public string Website { get; set; }
        public string Channel { get; set; }
        public EnvironmentModel Environment { get; set; }
        public ScrapperController Scrapper { get; set; }

        public Stream(string website, string channel, EnvironmentModel environment, ScrapperController scrapper)
        {
            Website = website;
            Channel = channel;
            Environment = environment;
            Scrapper = scrapper;
        }
    }
}
