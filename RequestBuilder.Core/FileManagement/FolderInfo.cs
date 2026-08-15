using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RequestBuilder.FileManagement
{
    public class FolderInfo : EntryInfo
    {
        public override bool IsDirectory => true;
    }
}
