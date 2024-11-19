using System;
using System.Runtime.Serialization;

namespace RequestBuilder.Errors {
    [Serializable]
    public class AuthException : BaseException {
        public AuthException() { }
        public AuthException(string message) : base(message) { }
        public AuthException(string message, Exception inner) : base(message, inner) { }
        protected AuthException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}