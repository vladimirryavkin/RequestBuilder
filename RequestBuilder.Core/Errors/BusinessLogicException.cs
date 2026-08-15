using RequestBuilder.Errors;
using System;
using System.Runtime.Serialization;

namespace RequestBuilder
{
    public class BusinessLogicException : BaseException
    {
        public BusinessLogicException() { }
        public BusinessLogicException(string message) : base(message) { }
        public BusinessLogicException(string message, Exception inner) : base(message, inner) { }
        protected BusinessLogicException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        public object ResponseModel { get; set; }
    }
}
