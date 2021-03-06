﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ModelSwapper
{
    static class Hasher
    {
        public static int GetHash(string val)
        {
            val = val.ToLowerInvariant();
            var hashtable = MemoryMarshal.Cast<byte, int>((ReadOnlySpan<byte>)new byte[]
            {
               0x7F,0x00,0x00,0x00,0x2B,0x03,0x00,0x00,0x3D,0x06,0x00,0x00,0x53,0x08,0x00,0x00,0xBD,0x0A,0x00,0x00,0x97,0x12,0x00,0x00,0x97,
               0x15,0x00,0x00,0x41,0x17,0x00,0x00,0xB5,0x1F,0x00,0x00,0x43,0x25,0x00,0x00,0x21,0x28,0x00,0x00,0x01,0x2A,0x00,0x00,0x97,0x2B,
               0x00,0x00,0x0D,0x30,0x00,0x00,0xA1,0x33,0x00,0x00,0x7F,0x37,0x00,0x00,0x35,0x3C,0x00,0x00,0x11,0x45,0x00,0x00,0xE5,0x48,0x00,
               0x00,0x45,0x4A,0x00,0x00,0x61,0x52,0x00,0x00,0x23,0x56,0x00,0x00,0x17,0x62,0x00,0x00,0xC9,0x64,0x00,0x00,0x41,0x6B,0x00,0x00,
               0x99,0x6D,0x00,0x00,0x8D,0x73,0x00,0x00,0x59,0x78,0x00,0x00,0x63,0x7F,0x00,0x00,0xA5,0x86,0x00,0x00,0xE3,0x8C,0x00,0x00,0x87,
               0x92,0x00,0x00,0x43,0x97,0x00,0x00,0x9D,0x9C,0x00,0x00,0xFF,0xA3,0x00,0x00,0x39,0xA9,0x00,0x00,0x1B,0xB0,0x00,0x00,0x47,0xB9,
               0x00,0x00,0x03,0xC2,0x00,0x00,0x4F,0xC6,0x00,0x00,0xCD,0xD0,0x00,0x00,0xAD,0xD8,0x00,0x00,0x69,0xDF,0x00,0x00,0xE9,0xE7,0x00,
               0x00,0x23,0xF2,0x00,0x00,0x2F,0xFE,0x00,0x00,0xCD,0x1E,0x01,0x00,0x19,0x30,0x01,0x00,0xFF,0x48,0x01,0x00,0xB1,0x5B,0x01,0x00
            });
            int hash = 0;
            for (int i = val.Length - 1; i > -1; i--)
            {
                int c = val[i];
                hash += (i + 1) * hashtable[c % hashtable.Length] + (c * hashtable[i % hashtable.Length]);
            }

            return hash;
        }

    }
}
