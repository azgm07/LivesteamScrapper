using ScrapperLibrary.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrapperLibrary.Controllers
{
    internal class QueueController
    {
        public ConcurrentQueue<QueueFunc> StartQueue { get; private set; }

        public QueueController()
        {
            StartQueue = new();
        }
    }
}
