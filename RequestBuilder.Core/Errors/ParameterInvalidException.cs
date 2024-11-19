using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RequestBuilder.Errors
{
    [Serializable]
    public class ParameterInvalidException : DebugException
    {
        public ParameterInvalidException() { }
        public ParameterInvalidException(string message) : base(message) { }
        public ParameterInvalidException(string message, Exception inner) : base(message, inner) { }
        protected ParameterInvalidException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
