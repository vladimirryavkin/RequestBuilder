using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RequestBuilder.Errors
{
    [Serializable]
    public class AccessDeniedException : WarningException
    {
        public AccessDeniedException() { }
        public AccessDeniedException(string message) : base(message) { }
        public AccessDeniedException(string message, Exception inner) : base(message, inner) { }
        protected AccessDeniedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
