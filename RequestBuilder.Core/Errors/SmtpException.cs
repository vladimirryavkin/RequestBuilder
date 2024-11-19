using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RequestBuilder.Errors {
    [Serializable]
    public class SmtpException : BaseException {
        public SmtpException() { }
        public SmtpException(String message) : base(message) { }
        public SmtpException(String message, Exception inner) : base(message, inner) { }
        protected SmtpException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
