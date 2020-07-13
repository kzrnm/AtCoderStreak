using System.IO;
using System.Text;

namespace AtCoderStreak.TestUtil
{
    public static class Util
    {
        public static MemoryStream StringToStream(string str)
            => StringToStream(str, new UTF8Encoding(false));
        public static MemoryStream StringToStream(string str, Encoding encoding)
            => new MemoryStream(encoding.GetBytes(str));
    }
}
