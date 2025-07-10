using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.ServerCore.Content
{
    public class StreamingData
    {
        public Stream? Stream { get; set; }
        public string? File { get; set; }
        public DateTime Deadline { get; set; }
    }
}
