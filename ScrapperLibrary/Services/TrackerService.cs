using ScrapperLibrary.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrapperLibrary.Services
{
    public class TrackerService
    {
        public List<TrackerController> ScopedInstances { get; private set; }

        public TrackerService()
        {
            ScopedInstances = new();
        }
    }
}
