using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrapperLibrary.Models
{
    internal class QueueFunc
    {
        public int Index { get; set; }
        public Func<Task> FuncTask { get; set; }

        public QueueFunc(int index, Func<Task> funcTask)
        {
            Index = index;
            FuncTask = funcTask;
        }
    }
}
