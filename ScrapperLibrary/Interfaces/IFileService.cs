
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrapperLibrary.Interfaces
{
    public interface IFileService
    {
        public void WriteFile(string folder, string file, List<string> lines, bool erase = false);
        public List<string> ReadFile(string folder, string file);
        public bool FileExists(string folder, string file);
    }
}
