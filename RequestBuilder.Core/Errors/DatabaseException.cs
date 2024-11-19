using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace RequestBuilder.Errors {
    [Serializable]
    public class DatabaseException : BaseException {
        public DatabaseException() { }
        public DatabaseException(string message) : base(message) { }
        public DatabaseException(string message, Exception inner) : base(message, inner) { }
        protected DatabaseException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
