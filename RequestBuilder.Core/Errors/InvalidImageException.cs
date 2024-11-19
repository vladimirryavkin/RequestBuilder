using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RequestBuilder.Errors {
    [Serializable]
    public class InvalidImageException:BaseException {
        public InvalidImageException() { }
        public InvalidImageException(String message) : base(message) { }
        public InvalidImageException(String message, Exception ex) : base(message, ex) { }
        protected InvalidImageException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
