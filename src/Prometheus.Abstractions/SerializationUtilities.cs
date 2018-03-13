using System;
using System.Collections.Generic;
using System.Text;

namespace Prometheus.Abstractions
{
    internal static class SerializationUtilities
    {
        public static string Identation(int depth)
        {
            if(depth < 1)
            {
                return string.Empty;
            }

            return new string(' ', depth * 2);
        }
    }

}