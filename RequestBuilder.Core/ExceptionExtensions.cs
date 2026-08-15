using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RequestBuilder
{
    public static class ExceptionExtensions
    {
		public static string ToText(this Exception ex)
        {
            var sb = new StringBuilder();
            var error = ex;
			while (error != null)
            {
				AppendException(error, sb);
                error = error.InnerException;
			}
            return sb.ToString();
        }

        private static void AppendException(Exception ex, StringBuilder sb)
        {
            sb.Append(ex.Message).AppendLine();
            sb.Append(ex.StackTrace).AppendLine();
            sb.Append("----------------").AppendLine();
        }
    }
}
