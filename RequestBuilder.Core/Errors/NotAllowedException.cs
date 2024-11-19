using System;
using System.Runtime.Serialization;

namespace RequestBuilder.Errors {
    [Serializable]
    public class NotAllowedException : BaseException {
        public NotAllowedException() { }
        public NotAllowedException(String message) : base(message) { }
        public NotAllowedException(String message, Exception inner) : base(message, inner) { }
        protected NotAllowedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}