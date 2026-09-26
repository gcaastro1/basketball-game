using System;
using System.IO;
using System.IO.Compression;

namespace Basket.EditorTools
{
    // Reads a binary FBX just far enough to find when its animation curves start and end.
    // The basketball mocap files declare takes far longer than their motion (RunningStraight:
    // a 88.5 s take whose curves end at 0.93 s; the jump shot: 11 s vs 6.6 s), and Unity sizes
    // the clip from the take -- a run loop that moves for 1 s and stands frozen for 87.
    public static class FbxCurveSpan
    {
        public const long TicksPerSecond = 46186158000L;

        // Seconds of the first and last animation key; false if the file has no curves or is
        // not a binary FBX.
        public static bool TryRead(byte[] data, out double start, out double end)
        {
            start = end = 0;
            if (data == null || data.Length < 27 || !StartsWith(data, "Kaydara FBX Binary")) return false;
            int version = BitConverter.ToInt32(data, 23);
            bool wide = version >= 7500;
            long first = long.MaxValue, last = long.MinValue;
            int offset = 27;
            while (offset < data.Length - 13)
            {
                long next = ReadNode(data, offset, wide, 0, ref first, ref last);
                if (next <= offset) break;
                offset = (int)next;
            }
            if (first == long.MaxValue)
            {
                start = end = 0;
                return false;
            }
            start = first / (double)TicksPerSecond;
            end = last / (double)TicksPerSecond;
            return true;
        }

        public static bool TryRead(string path, out double start, out double end)
        {
            start = end = 0;
            return File.Exists(path) && TryRead(File.ReadAllBytes(path), out start, out end);
        }

        // Returns the offset after this node (0 for the null record that ends a list).
        private static long ReadNode(byte[] b, int o, bool wide, int depth, ref long first, ref long last)
        {
            long endOffset, count, listLength;
            if (wide)
            {
                endOffset = BitConverter.ToInt64(b, o);
                count = BitConverter.ToInt64(b, o + 8);
                listLength = BitConverter.ToInt64(b, o + 16);
                o += 24;
            }
            else
            {
                endOffset = BitConverter.ToUInt32(b, o);
                count = BitConverter.ToUInt32(b, o + 4);
                listLength = BitConverter.ToUInt32(b, o + 8);
                o += 12;
            }
            int nameLength = b[o];
            o += 1;
            if (endOffset == 0) return 0;
            string name = System.Text.Encoding.ASCII.GetString(b, o, nameLength);
            o += nameLength;

            int propsStart = o;
            if (name == "KeyTime" && count >= 1 && b[o] == (byte)'l')
            {
                long[] times = ReadLongArray(b, o + 1);
                if (times != null && times.Length > 0)
                {
                    if (times[0] < first) first = times[0];
                    if (times[times.Length - 1] > last) last = times[times.Length - 1];
                }
            }
            o = (int)(propsStart + listLength);

            // Children, up to the end of this node.
            while (o < endOffset - (wide ? 25 : 13) && depth < 64)
            {
                long next = ReadNode(b, o, wide, depth + 1, ref first, ref last);
                if (next == 0) break;
                o = (int)next;
            }
            return endOffset;
        }

        private static long[] ReadLongArray(byte[] b, int o)
        {
            int length = BitConverter.ToInt32(b, o);
            int encoding = BitConverter.ToInt32(b, o + 4);
            int compressed = BitConverter.ToInt32(b, o + 8);
            o += 12;
            byte[] raw;
            if (encoding == 1)
            {
                // zlib: 2-byte header, then deflate.
                using (var input = new MemoryStream(b, o + 2, compressed - 2))
                using (var deflate = new DeflateStream(input, CompressionMode.Decompress))
                using (var output = new MemoryStream())
                {
                    deflate.CopyTo(output);
                    raw = output.ToArray();
                }
            }
            else
            {
                raw = new byte[compressed];
                Array.Copy(b, o, raw, 0, compressed);
            }
            if (raw.Length < length * 8) return null;
            var values = new long[length];
            for (int i = 0; i < length; i++) values[i] = BitConverter.ToInt64(raw, i * 8);
            return values;
        }

        private static bool StartsWith(byte[] b, string s)
        {
            for (int i = 0; i < s.Length; i++)
                if (b[i] != (byte)s[i]) return false;
            return true;
        }
    }
}
