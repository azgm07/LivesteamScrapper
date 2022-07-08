using ScrapperLibrary.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ScrapperLibrary.Models.Enums;

namespace ScrapperLibrary.Models
{
    public class Instance
    {
        public StreamStatus Status { get; set; }
        public TrackerController Tracker { get; set; }

        public Instance(StreamStatus status, TrackerController tracker)
        {
            Status = status;
            Tracker = tracker;
        }
    }
}
