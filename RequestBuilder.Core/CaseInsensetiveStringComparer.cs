﻿using System;
using System.Collections.Generic;
namespace RequestBuilder
{
    public class CaseInsensetiveStringComparer : IEqualityComparer<String>
    {
        public bool Equals(String x, String y)
        {
            return String.Equals(x, y, StringComparison.OrdinalIgnoreCase);
        }
        public int GetHashCode(String obj)
        {
            if (obj != null)
                return obj.ToLower().GetHashCode();
            return 0;
        }
    }
}