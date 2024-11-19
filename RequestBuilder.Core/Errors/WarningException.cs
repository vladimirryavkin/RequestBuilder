using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RequestBuilder.Errors
{
    [Serializable]
    public class WarningException : BaseException
    {
        public WarningException() { }
        public WarningException(string message) : base(message) { }
        public WarningException(string message, Exception inner) : base(message, inner) { }
        protected WarningException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
