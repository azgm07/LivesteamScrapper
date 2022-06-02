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
        public static void UpdateCsv(string folder, string file, List<string> lines)
        {
            string sCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string sPath = System.IO.Path.Combine(sCurrentDirectory, folder);
            string sFile = System.IO.Path.Combine(sPath, file);
            string sFullPath = Path.GetFullPath(sPath);
            string sFilePath = Path.GetFullPath(sFile);

            if (!Directory.Exists(sFullPath))
            {
                Directory.CreateDirectory(sFullPath);
            }

            using FileStream fs = new(sFilePath, FileMode.Append, FileAccess.Write);
            using StreamWriter sw = new(fs);
            foreach (string line in lines)
            {
                sw.WriteLine(line);
            }
        }

        public static void WriteCsv(string folder, string file, List<string> lines)
        {
            string sCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string sPath = System.IO.Path.Combine(sCurrentDirectory, folder);
            string sFile = System.IO.Path.Combine(sPath, file);
            string sFullPath = Path.GetFullPath(sPath);
            string sFilePath = Path.GetFullPath(sFile);

            if (!Directory.Exists(sFullPath))
            {
                Directory.CreateDirectory(sFullPath);
            }

            StringBuilder sb = new();
            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }

            System.IO.File.WriteAllText(sFilePath, sb.ToString());
        }

        public static List<string> ReadCsv(string folder, string file)
        {
            string filePath = System.IO.Path.Combine(folder, file);
            string sCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string sPath = System.IO.Path.Combine(sCurrentDirectory, folder);
            string sFile = System.IO.Path.Combine(sCurrentDirectory, filePath);
            string sFullPath = Path.GetFullPath(sPath);
            string sFilePath = Path.GetFullPath(sFile);

            if (!Directory.Exists(sFullPath))
            {
                Directory.CreateDirectory(sFullPath);
            }

            if (!System.IO.File.Exists(sFilePath))
            {
                WriteCsv(folder, file, new List<string>());
            }

            using var reader = new StreamReader(sFilePath);
            List<string> list = new List<string>();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();

                if (!string.IsNullOrEmpty(line))
                {
                    list.Add(line);
                }
            }
            return list;
        }
    }
}