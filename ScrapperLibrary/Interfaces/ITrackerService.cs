using ScrapperLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrapperLibrary.Interfaces
{
    public interface ITrackerService
    {
        List<Instance> TrackerInstances { get; }
        int TrackerSeconds { get; set; }
        int WaitSeconds { get; set; }
        CancellationToken CurrentToken { get; }

        bool AddInstance(string website, string channel, bool start = true, bool save = false);
        bool RemoveInstance(string website, string channel, bool saveFile = true);
        Task RunTrackerAsync(List<string> streams, CancellationToken token);
        bool StartInstance(string website, string channel);
        bool StopInstance(string website, string channel);
        void StartAllInstances();
        void StopAllInstances();
    }
}
