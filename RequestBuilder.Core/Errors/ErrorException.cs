using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RequestBuilder.Errors
{
    [Serializable]
    public class ErrorException : BaseException
    {
        public ErrorException() { }
        public ErrorException(string message) : base(message) { }
        public ErrorException(string message, Exception inner) : base(message, inner) { }
        protected ErrorException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
