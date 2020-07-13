using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ModelSwapper
{
    enum Fab
    {
        Torso,
        Legs,
        Feet,
        Hands,
        //Head
    }

    static class Program
    {
        static Dictionary<uint, string> FabDictById = File.ReadAllLines("fab.csv").Skip(1)
                .ToDictionary(line => uint.Parse(line[..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                line => line[7..]);

        static Dictionary<string, uint> FabDictByName = FabDictById.ToDictionary(x => x.Value, x => x.Key, StringComparer.OrdinalIgnoreCase);


        //static (int id, string name, byte[] data) BuildTuple(Fab fab, string name, int? loc= null)
        //    => (loc ?? Hasher.GetHash(name), name, LoadModifiedSimtype(fab));


        static byte[] BuildSimtypeMgr(IEnumerable<int> entries)
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


        static byte[] CreateModifiedSimType(Fab fab, string maleName, string femaleName)
        {
            var bytes = File.ReadAllBytes($"{fab}.simtype_bxml");
            ReadOnlySpan<byte> target = new byte[8] { 0x07, 0, 0, 0, 0, 0, 0, 0 };
            var ix = bytes.AsSpan().IndexOf(target);
            if (FabDictByName.TryGetValue($"{maleName}{fab}", out var maleId))
            {
                bytes.Write(ix + 9, maleId);
            }
             if (FabDictByName.TryGetValue($"{femaleName}{fab}", out var femaleId))
            { 

                bytes.Write(ix + 13, femaleId);
            }
            return bytes;
        }

        static (int id, string name, byte[] data)[] BuildTable(string maleName, string femaleName, string baseSimType = "Clothing_peasant03_")
        => ((Fab[])typeof(Fab).GetEnumValues())
                .Select(f => ($"{baseSimType}{f}", CreateModifiedSimType(f, maleName, femaleName)))
                .Select(f => (Hasher.GetHash(f.Item1), f.Item1, f.Item2)).ToArray();
            
            
        

        //static byte[] LoadModifiedSimtype(Fab fab)
        //{
        //    //todo.
        //    var bytes =  File.ReadAllBytes($"{fab}.simtype_bxml");
        //    var maleReplacement = FabDictById[maleFabId].ToLowerInvariant().Replace("almain", "Dokkalfar").Replace("varani", "Dokkalfar");
        //    var femaleReplacement = FabDictById[femaleFabId].ToLowerInvariant().Replace("almain", "Dokkalfar").Replace("varani", "Dokkalfar");
        //    if (fab != Fab.Head && fab != Fab.Robe)
        //    {
        //        var newMaleId = FabDictById.First(x => x.Value.Equals(maleReplacement, StringComparison.OrdinalIgnoreCase)).Key;
        //        bytes.Write(ix + 9, newMaleId);
        //    }
        //    var newFemaleId = fab == Fab.Robe ? 1590861
        //        : FabDictById.First(x => x.Value.Equals(femaleReplacement, StringComparison.OrdinalIgnoreCase)).Key;
        //    bytes.Write(ix + 13, newFemaleId);

        //    return bytes;
        //}


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
                var hash = Hasher.GetHash(name);
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
            
            var simtypeTable = BuildTable("splinter_02_melee_Set_Unique_", "splinter_02_melee_Set_Unique_f_");
            // simtypeMGR
            var simtypeMgr = BuildSimtypeMgr(simtypeTable.Select(x => x.id));
            File.WriteAllBytes("simtype_mgr.bundle", simtypeMgr);
            File.WriteAllBytes(bundlePath, BuildBigFile((0x_03_FF_6F_48, simtypeMgr, 0x14)));

            // simtype init table
            var simtypeInitFile = BuildSimtypeInit(simtypeTable.Select(x => (x.id, x.name)).ToArray());
            File.WriteAllBytes("simtype_init.bin", simtypeInitFile);
            // simtype bundle.
            File.WriteAllBytes(patchPath,
              BuildBigFile(
                  new[] {
                   (0x01_CA_82_FD, simtypeInitFile, 0x14)  }
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

        static T Read<T>(this byte[] bytes, int offset)
            where T : struct
         => ((ReadOnlySpan<byte>)bytes).Read<T>(offset);
    }
}
