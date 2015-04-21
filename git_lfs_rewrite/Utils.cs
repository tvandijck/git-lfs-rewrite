using System;
using System.IO;
using System.Linq;
using System.Text;

static internal class Utils
{
    public static string ReadString(Stream str)
    {
        var result = String.Empty;
        for (; ; )
        {
            var c = str.ReadByte();
            if (c <= 0)
                return result;
            result += (char)c;
        }
    }

    public static string ReadSHA1(Stream str)
    {
        var result = String.Empty;
        for (var i = 0; i < 20; ++i)
        {
            var c = str.ReadByte();
            result += string.Format("{0:x2}", c);
        }
        return result;
    }

    internal static string ToHex(byte[] sha)
    {
        return sha.Aggregate(String.Empty, (current, b) => current + string.Format("{0:x2}", b));
    }

    internal static void WriteString(Stream stream, string str)
    {
        var d = Encoding.ASCII.GetBytes(str);
        stream.Write(d, 0, d.Length);
        stream.WriteByte(0);
    }

    internal static void WriteSHA1(Stream stream, string str)
    {
        for (var i = 0; i < 20; ++i)
        {
            var b = Convert.ToByte(str.Substring(i * 2, 2), 16);
            stream.WriteByte(b);
        }
    }
}