using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ModelSwapper
{
    enum Fab
    {
        Torso,
        Head,
        Legs,
        Feet
    }

    static class Program
    {
        static int GetHash(string val)
        {
            val = val.ToLowerInvariant();
            ReadOnlySpan<byte> data = new byte[]
            {
                0x7F,0x00,0x00,0x00,0x2B,0x03,0x00,0x00,0x3D,0x06,0x00,0x00,0x53,0x08,0x00,0x00,0xBD,0x0A,0x00,0x00,0x97,0x12,0x00,0x00,0x97,
                0x15,0x00,0x00,0x41,0x17,0x00,0x00,0xB5,0x1F,0x00,0x00,0x43,0x25,0x00,0x00,0x21,0x28,0x00,0x00,0x01,0x2A,0x00,0x00,0x97,0x2B,
                0x00,0x00,0x0D,0x30,0x00,0x00,0xA1,0x33,0x00,0x00,0x7F,0x37,0x00,0x00,0x35,0x3C,0x00,0x00,0x11,0x45,0x00,0x00,0xE5,0x48,0x00,
                0x00,0x45,0x4A,0x00,0x00,0x61,0x52,0x00,0x00,0x23,0x56,0x00,0x00,0x17,0x62,0x00,0x00,0xC9,0x64,0x00,0x00,0x41,0x6B,0x00,0x00,
                0x99,0x6D,0x00,0x00,0x8D,0x73,0x00,0x00,0x59,0x78,0x00,0x00,0x63,0x7F,0x00,0x00,0xA5,0x86,0x00,0x00,0xE3,0x8C,0x00,0x00,0x87,
                0x92,0x00,0x00,0x43,0x97,0x00,0x00,0x9D,0x9C,0x00,0x00,0xFF,0xA3,0x00,0x00,0x39,0xA9,0x00,0x00,0x1B,0xB0,0x00,0x00,0x47,0xB9,
                0x00,0x00,0x03,0xC2,0x00,0x00,0x4F,0xC6,0x00,0x00,0xCD,0xD0,0x00,0x00,0xAD,0xD8,0x00,0x00,0x69,0xDF,0x00,0x00,0xE9,0xE7,0x00,
                0x00,0x23,0xF2,0x00,0x00,0x2F,0xFE,0x00,0x00,0xCD,0x1E,0x01,0x00,0x19,0x30,0x01,0x00,0xFF,0x48,0x01,0x00,0xB1,0x5B,0x01,0x00
            };
            var hashtable = MemoryMarshal.Cast<byte, int>(data);
            int hash = 0;
            for (int i = val.Length - 1; i > -1; i--)
            {
                int c = val[i];
                hash += (i + 1) * hashtable[c % 50] + (c * hashtable[i % 50]);
            }
            
           return hash;
        }

        static (int id, string name, byte[] data) BuildTuple(Fab fab, string name, int? loc= null)
            => (loc ?? GetHash(name), name, LoadModifiedSimtype(fab));


        static byte[] BuildSimtypeMgrBundle(IEnumerable<int> entries)
        {
            static byte[] WriteSimtypeMgrInternal(int[] entries)
            {
                var output = new byte[16 + entries.Length * 7];
                output.Write(8, entries.Length);
                for (int i = 0; i < entries.Length; i++)
                {
                    output.Write(16 + i * 4, entries[i]);
                    output[16 + (entries.Length * 4) + i] = 0x0d;
                    output[16 + (entries.Length * 6) + i] = (byte)(i < 2 ? 1 : 0);
                }
                return output;
            }
            return WriteSimtypeMgrInternal(new[] {unchecked((int)0x80_00_1F_C2u), unchecked((int)0x80_1d_80_74u) }.Concat(entries).ToArray());
        }

        static byte[] LoadModifiedSimtype(Fab fab)
        {
            //todo.
            return File.ReadAllBytes($"{fab}.simtype_bxml");
        }

        static byte[] BuildSimtypeInit(params (int simtypeId, string name)[] newNames)
        {
            ReadOnlySpan<byte> data = File.ReadAllBytes("simtype_init.bin");
            var newData = new byte[data.Length + newNames.Length * 8];
            var currentLen = data.Read<int>(4);
            var listSize = currentLen * 4;
            var eol1 = listSize + 8;
            var eol2 = eol1 + newNames.Length * 4 + listSize;
            data.Slice(0, eol1).CopyTo(newData);
            data.Slice(eol1).CopyTo(newData.AsSpan(eol1 + newNames.Length * 4));
            newData.Write(4, currentLen + newNames.Length);

            foreach (var (id, name) in newNames)
            {
                var hash = GetHash(name);
                newData.Write(eol1, id);
                newData.Write(eol2, hash);
                eol1 += 4;
                eol2 += 4;
            }
            return newData;
        }

        static byte[] BuildBigFile(params (int id, byte[] data, int flag)[] fileTable)
        {
            ReadOnlySpan<byte> header = new byte[] {
                0x80, 0xC7, 0xC8, 0xC2, 0x10, 0x00, 0x00, 0x00, 0x01, 0x1E, 0x00, 0x00, 0x00, 0x42, 0x48, 0x47,
                0x36, 0x31, 0x32, 0x30, 0x20, 0x3A, 0x20, 0x33, 0x38, 0x43, 0x4F, 0x52, 0x50, 0x20, 0x3A, 0x20,
                0x62, 0x68, 0x67, 0x2E, 0x62, 0x75, 0x69, 0x6C, 0x64, 0x65, 0x72, 0x20, 0x00, 0x00, 0x00 };
            var bytes = new byte[header.Length + 4 + 20 * fileTable.Length + fileTable.Sum(x => x.data.Length)];
            header.CopyTo(bytes);
            bytes.Write(header.Length, fileTable.Length);
            var currentOffset = header.Length + 4;
            var fileOffset = currentOffset + (20 * fileTable.Length);
            foreach (var (id, data, flag) in fileTable)
            {
                bytes.Write(currentOffset, data.Length);
                bytes.Write(currentOffset + 4, data.Length);
                bytes.Write(currentOffset + 8, fileOffset);
                bytes.Write(currentOffset + 12, id);
                bytes.Write(currentOffset + 16, flag);
                data.CopyTo(bytes.AsSpan(fileOffset));
                fileOffset += data.Length;
                currentOffset += 20;
            }
            return bytes;
        }

        static void Main(string[] args)
        {
            const string bundlePath = @"C:\Program Files (x86)\Steam\steamapps\common\KOAReckoning\bigs\001\BundleTarget\generated_patch.big";
            const string patchPath = @"C:\Program Files (x86)\Steam\steamapps\common\KOAReckoning\bigs\001\Patches\zpatch.big";

            var simtypeTable = new (int id, string name, byte[] data)[]
            {
                BuildTuple(Fab.Torso, "mburbea_clothing_peasant03_torso", 0x_1F_E6_77),
                //BuildTuple(201, id:677345, Fab.Head, "Clothing_Peasant03_Legs"),
                //BuildTuple(202, id:677346, Fab.Legs, "Clothing_Peasant03_Head"),
                //BuildTuple(203, id:677347, Fab.Feet, "Clothing_Peasant03_Feet")
            };
            // simtypeMGR
            var simtypeMgr = BuildSimtypeMgrBundle(simtypeTable.Select(x => x.id));
            File.WriteAllBytes("simtype_mgr.bundle", simtypeMgr);
            File.WriteAllBytes(bundlePath, BuildBigFile((0x_03_FF_6F_48, simtypeMgr, 0x14)));

            // simtype init table
            var simtypeInitFile = BuildSimtypeInit(simtypeTable.Select(x => (x.id, x.name)).ToArray());
            File.WriteAllBytes("simtype_init.bin", simtypeInitFile);
            // simtype bundle.
            File.WriteAllBytes(patchPath,
              BuildBigFile(
                  new[] { (0x0, Array.Empty<byte>(), 0x90), (0x0172fb46, simtypeInitFile, 0x14)  }
                  .Concat(
                    simtypeTable.Select(x => (x.id, x.data, 0x94))
                    )
                    .ToArray()));

        }

        static void Write<T>(this byte[] bytes, int offset, T value)
            where T : struct
         => MemoryMarshal.Write(bytes.AsSpan(offset), ref value);

        static T Read<T>(this ReadOnlySpan<byte> bytes, int offset)
            where T : struct
         => MemoryMarshal.Read<T>(bytes.Slice(offset));
    }
}
