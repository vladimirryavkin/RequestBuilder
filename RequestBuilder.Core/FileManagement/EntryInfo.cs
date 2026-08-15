using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RequestBuilder.FileManagement
{
    public abstract class EntryInfo
    {
        public string FullPath { get; set; }
        public string Name { get; set; }
        public abstract bool IsDirectory { get; }
    }
}
