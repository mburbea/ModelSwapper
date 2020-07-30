using System;
using System.Runtime.InteropServices;

namespace ModelSwapper
{
    public static class MemoryUtilities
    {
        public static void Write<T>(this byte[] bytes, int offset, T value)  where T : struct
         => MemoryMarshal.Write(bytes.AsSpan(offset), ref value);

        public static T Read<T>(this ReadOnlySpan<byte> bytes, int offset)
            where T : struct
         => MemoryMarshal.Read<T>(bytes.Slice(offset));

        public static T Read<T>(this byte[] bytes, int offset)
            where T : struct
         => ((ReadOnlySpan<byte>)bytes).Read<T>(offset);
    }
}
