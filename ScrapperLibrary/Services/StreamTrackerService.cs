using ScrapperLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrapperLibrary.Services
{
    public class StreamTrackerService
    {
        public TrackerResponse LastResponse { get; private set; }

        public StreamTrackerService()
        {
            LastResponse = new();
        }

        public TrackerResponse GetInfo()
        {
            TrackerResponse response = new();

            //Implement coding

            LastResponse = response;
            return response;
        }
    }
}
