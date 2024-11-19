using System;
using System.Runtime.Serialization;
namespace RequestBuilder.Errors {
    [Serializable]
    public class RemoteServerProcessingException : BaseException {
        public RemoteServerProcessingException() { }
        public RemoteServerProcessingException(string message) : base(message) { }
        public RemoteServerProcessingException(string message, Exception inner) : base(message, inner) { }
        protected RemoteServerProcessingException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}