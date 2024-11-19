using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RequestBuilder.Errors
{
    [Serializable]
    public class DebugException : BaseException
    {
        public DebugException() { }
        public DebugException(string message) : base(message) { }
        public DebugException(string message, Exception inner) : base(message, inner) { }
        protected DebugException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
