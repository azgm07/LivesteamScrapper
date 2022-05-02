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
        public static void WriteToCsv(string filename, List<string> lines)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Append, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    foreach(string line in lines)
                    {
                        sw.WriteLine(line);
                    }
                }
            }

            //StringBuilder sb = new StringBuilder();
            //foreach (var line in lines)
            //{
            //    sb.AppendLine(line);
            //}

            //System.IO.File.WriteAllText(filename, sb.ToString());
        }
    }
}