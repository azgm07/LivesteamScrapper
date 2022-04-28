using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace LivesteamScrapper.Controllers
{
    public class FileController : Controller
    {
        private readonly ILogger<Controller> _logger;

        public FileController(ILogger<Controller> logger)
        {
            _logger = logger;
        }

        //Write CSV lines with a list of strings
        public void WriteToCsv(List<string> lines)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }

            System.IO.File.WriteAllText("file.csv", sb.ToString());
        }
    }
}