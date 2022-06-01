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
        public static void UpdateCsv(string filename, List<string> lines)
        {
            using FileStream fs = new(filename, FileMode.Append, FileAccess.Write);
            using StreamWriter sw = new (fs);
            foreach (string line in lines)
            {
                sw.WriteLine(line);
            }
        }

        public static void WriteCsv(string filename, List<string> lines)
        {
            StringBuilder sb = new();
            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }

            System.IO.File.WriteAllText(filename, sb.ToString());
        }
    }
}