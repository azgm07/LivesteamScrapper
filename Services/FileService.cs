namespace LivesteamScrapper.Services
{
    public interface IFileService
    {
        public void WriteCsv(string folder, string file, List<string> lines, bool erase = false);
        public List<string> ReadCsv(string folder, string file);
    }
    public class FileService : IFileService
    {
        private readonly ILogger<FileService> _logger;

        public FileService(ILogger<FileService> logger)
        {
            _logger = logger;
        }

        //Write CSV lines with a list of strings
        public void WriteCsv(string folder, string file, List<string> lines, bool erase = false)
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

            if (erase && System.IO.File.Exists(sFilePath))
            {
                System.IO.File.Delete(sFilePath);
            }

            using FileStream fs = new(sFilePath, FileMode.Append, FileAccess.Write);
            using StreamWriter sw = new(fs);
            foreach (string line in lines)
            {
                sw.WriteLine(line);
            }

            _logger.LogInformation("File saved in {file}.", sFilePath);
        }

        public List<string> ReadCsv(string folder, string file)
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

            if (System.IO.File.Exists(sFilePath))
            {
                using var reader = new StreamReader(sFilePath);
                List<string> list = new();
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
            return new();
        }
    }
}
