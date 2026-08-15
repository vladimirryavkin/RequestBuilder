using System;
using System.Text;

namespace RequestBuilder
{
    public class PostValueParam : IMultipartParameter
    {
        public string ParamName { get; private set; }
        public string Value { get; private set; }
        public PostValueParam(string paramName, string value)
        {
            Guard.PropertyNotNullOrEmpty(paramName, "paramName");
            Guard.PropertyNotNull(value, "value");
            ParamName = paramName;
            Value = value;
        }
        byte[] IMultipartParameter.Value
        {
            get { return Value.Return(x => Encoding.UTF8.GetBytes(Value), null); }
        }
        string IMultipartParameter.FileName
        {
            get { return null; }
        }
        string IMultipartParameter.ContentType
        {
            get { return null; }
        }
    }
}