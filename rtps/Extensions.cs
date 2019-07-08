using System;
using System.Text;
using NUnit.Framework.Constraints;

namespace rtps {
    public static class Extensions {
        public static string ToString(this byte[] bytes, string delim) {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < bytes.Length; i++) {
                sb.Append("0x");
                sb.Append(bytes[i].ToString("x2"));
                if (i < bytes.Length - 1) {
                    sb.Append(delim);
                }
            }
            sb.Append("]");
            return sb.ToString();
        }

        public static int HashCode(this byte[] array)
        {
            unchecked
            {
                int hash = 17;
                foreach (var value in array)
                {
                    hash = hash * 23 + value.GetHashCode();
                }

                return hash;
            }
        }
    }
}