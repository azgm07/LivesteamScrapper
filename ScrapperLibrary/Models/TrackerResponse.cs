using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ScrapperLibrary.Models.Enums;

namespace ScrapperLibrary.Models
{
    public class TrackerResponse
    {
        public string? CurrentGame { get; set; }
        public int CurrentViewers { get; set; }

    }
}
