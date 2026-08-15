using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RequestBuilder.FileManagement
{
    public class FileInfo : EntryInfo
    {
        public override bool IsDirectory => false;
    }
}
