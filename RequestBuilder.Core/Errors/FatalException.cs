using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RequestBuilder.Errors
{
    [Serializable]
    public class FatalException : BaseException
    {
        public FatalException() { }
        public FatalException(string message) : base(message) { }
        public FatalException(string message, Exception inner) : base(message, inner) { }
        protected FatalException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
