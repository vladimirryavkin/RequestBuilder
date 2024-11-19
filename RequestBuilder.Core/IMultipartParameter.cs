using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RequestBuilder {
    public interface IMultipartParameter {
        String ParamName { get; }
        byte[] Value { get; }
        String FileName { get; }
        String ContentType { get; }
    }
}
