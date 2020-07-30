﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace ModelSwapper
{
    enum Fab
    {
        Torso,
        Legs,
        Feet,
        Hands,
        Head
    }

    static class Program
    {
        static readonly Dictionary<string, uint> FabDictByName = File.ReadAllLines("fab.csv")
            .Skip(1)
            .ToDictionary(line => line[7..], line => uint.Parse(line[..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture), StringComparer.OrdinalIgnoreCase);
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


        static SimTypeAsset CreateModifiedSimType(Fab fab, string maleName, string femaleName, string baseSimName)
        {
            var bytes = File.ReadAllBytes($"{fab}.simtype_bxml");
            ReadOnlySpan<byte> target = new byte[8] { 0x07, 0, 0, 0, 0, 0, 0, 0 };
            var ix = bytes.AsSpan().IndexOf(target);
            bool didChanges = false;
            if (FabDictByName.TryGetValue($"{maleName}{fab}", out uint maleId))
            {
                didChanges = true;
                bytes.Write(ix + 9, maleId);
            }
            else
            {
                maleId = bytes.Read<uint>(ix + 9);
            }
            if (FabDictByName.TryGetValue($"{femaleName}{fab}", out uint femaleId))
            {
                didChanges = true;
                bytes.Write(ix + 13, femaleId);
            }
            else
            {
                femaleId = bytes.Read<uint>(ix + 13);
            }
            if (didChanges) {

                var name = $"{baseSimName}{fab}";
                var id = Hasher.GetHash(name);
                var bundleBytes = new byte[30];
                bundleBytes.Write(8, 2);
                bundleBytes.Write(16, maleId);
                bundleBytes.Write(20, femaleId);
                var bundleId = unchecked((int)0xF0_00_00_00) | id;
                var bundle2Id = 0x20_00_00_00 | id;
                return new SimTypeAsset(id, name, bytes, bundleId, bundleBytes, bundle2Id);            
            }
            return default;
        }

        static IEnumerable<SimTypeAsset> BuildTable(string maleName, string femaleName, string baseSimType = "Clothing_peasant03_")
        => ((Fab[])typeof(Fab).GetEnumValues())
                .Select(x => CreateModifiedSimType(x, maleName, femaleName, baseSimType))
                .Where(x => x.bytes != null);
            
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

        static byte[] BuildBigFile((int id, byte[] data, int flag)[] fileTable)
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
            FabDictByName.Add("01_generic_peasant_female_feet", FabDictByName["01_generic_female_peasant_feet"]);

            var simtypeTable = BuildTable("splinter_02_melee_Set_Unique_", "splinter_02_melee_Set_Unique_f_","glowing_warrior_")
                //.Concat(BuildTable("splinter_03_Rogue_Set_Unique_", "splinter_03_Rogue_Set_Unique_f_", "glowing_rogue_"))
                //.Concat(BuildTable("01_generic_male_peasant_", "01_generic_peasant_female_", "Clothing_peasant03_"))
                //.Concat(BuildTable("01_Dokkalfar_noble_male_", "01_Dokkalfar_noble_female_", "Clothing_peasant04_"))
                //.Concat(BuildTable("02_Dokkalfar_noble_male_", "02_Dokkalfar_noble_female_", "Clothing_peasant05_"))
                //.Concat(BuildTable("01_Dokkalfar_peasant_male_", "01_Dokkalfar_peasant_female_", "Clothing_peasant06_"))
                //.Concat(BuildTable("02_Dokkalfar_peasant_male_", "02_Dokkalfar_peasant_female_", "Clothing_peasant07_"))
                //.Concat(BuildTable("01_Dokkalfar_merchant_male_", "01_Dokkalfar_merchant_female_", "Clothing_peasant08_"))
                //.Concat(BuildTable("02_Dokkalfar_merchant_male_", "02_Dokkalfar_merchant_female_", "Clothing_peasant09_"))

                //.Concat(BuildTable("01_Ljosalfar_noble_male_", "01_Ljosalfar_noble_female_", "Clothing_peasant10_"))
                //.Concat(BuildTable("02_Ljosalfar_noble_male_", "02_Ljosalfar_noble_female_", "Clothing_peasant11_"))
                //.Concat(BuildTable("01_Ljosalfar_peasant_male_", "01_Ljosalfar_peasant_female_", "Clothing_peasant12_"))
                //.Concat(BuildTable("02_Ljosalfar_peasant_male_", "02_Ljosalfar_peasant_female_", "Clothing_peasant13_"))
                //.Concat(BuildTable("01_Ljosalfar_merchant_male_", "01_Ljosalfar_Merchant_f_", "Clothing_peasant14_"))
                //.Concat(BuildTable("02_Ljosalfar_merchant_male_", "02_Ljosalfar_Merchant_f_", "Clothing_peasant15_"))
                
                //.Concat(BuildTable("01_almain_merchant_male_", "01_almain_Merchant_female_", "Clothing_peasant16_"))
                //.Concat(BuildTable("02_almain_merchant_male_", "02_almain_Merchant_female_", "Clothing_peasant17_"))
                //.Concat(BuildTable("01_varani_merchant_male_", "01_varani_Merchant_female_", "Clothing_peasant18_"))
                //.Concat(BuildTable("02_varani_merchant_male_", "02_varani_Merchant_female_", "Clothing_peasant19_"))
                //.Concat(BuildTable("01_almain_noble_male_", "01_almain_noble_female_", "Clothing_peasant20_"))
                //.Concat(BuildTable("02_almain_noble_male_", "02_almain_noble_female_", "Clothing_peasant21_"))
                //.Concat(BuildTable("01_varani_noble_male_", "01_varani_noble_female_", "Clothing_peasant22_"))
                //.Concat(BuildTable("02_varani_noble_male_", "02_varani_noble_female_", "Clothing_peasant23_"))

                .ToArray();
            Directory.CreateDirectory(@"output\_scripts\console\");
            if (!Directory.Exists("output"))
            {
                Directory.CreateDirectory(@"output\bigs\001\BundleTarget\");
                Directory.CreateDirectory(@"output\bigs\001\Patches\");

            }
            File.Copy("createclothes.lua", @"output\_scripts\console\createclothes.lua", true);

            // simtypeMGR
            var simtypeMgr = BuildSimtypeMgr(simtypeTable.Select(x => x.id));
            File.WriteAllBytes("simtype_mgr.bundle", simtypeMgr);
            File.WriteAllBytes(bundlePath, BuildBigFile(
                new[] { (0x_03_FF_6F_48, simtypeMgr, 0x14) }
                .Concat(simtypeTable.SelectMany(x=> new[] {(x.bundleId, x.bundleBytes, 0x94), (x.bundle2Id, SimTypeAsset.Bundle2Bytes, 0x94)}))
                .ToArray()));

            // simtype init table
            var simtypeInitFile = BuildSimtypeInit(simtypeTable.Select(x => (x.id, x.name)).ToArray());
            File.WriteAllBytes("simtype_init.bin", simtypeInitFile);
            // simtype bundle.
            File.WriteAllBytes(patchPath,
              BuildBigFile(
                  new[] { (0x01_CA_82_FD, simtypeInitFile, 0x14)  }
                    .Concat(simtypeTable.Select(x=> (x.id, x.bytes, 0x94)))
                    .ToArray()));
            if (File.Exists("clothingmod.zip"))
            {
                File.Delete("clothingmod.zip");
            }
            ZipFile.CreateFromDirectory("output", "clothingmod.zip");
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

    internal struct SimTypeAsset
    {
        internal static byte[] Bundle2Bytes = { 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0x45, 0xAB, 0x1A, 0, 0, 0, 0, };
        public int id;
        public string name;
        public byte[] bytes;
        public int bundleId;
        public byte[] bundleBytes;
        public int bundle2Id;

        public SimTypeAsset(int id, string name, byte[] bytes, int bundleId, byte[] bundleBytes, int bundle2Id)
        {
            this.id = id;
            this.name = name;
            this.bytes = bytes;
            this.bundleId = bundleId;
            this.bundleBytes = bundleBytes;
            this.bundle2Id = bundle2Id;
        }

        public override bool Equals(object obj)
        {
            return obj is SimTypeAsset other &&
                   id == other.id &&
                   name == other.name &&
                   EqualityComparer<byte[]>.Default.Equals(bytes, other.bytes) &&
                   bundleId == other.bundleId &&
                   EqualityComparer<byte[]>.Default.Equals(bundleBytes, other.bundleBytes) &&
                   bundle2Id == other.bundle2Id;
        }


        public override int GetHashCode()
        {
            int hashCode = 1078890686;
            hashCode = hashCode * -1521134295 + id.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(name);
            hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(bytes);
            hashCode = hashCode * -1521134295 + bundleId.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(bundleBytes);
            hashCode = hashCode * -1521134295 + bundle2Id.GetHashCode();
            return hashCode;
        }

        public void Deconstruct(out int id, out string name, out byte[] bytes, out int bundleId, out byte[] bundleBytes, out int bundle2Id)
        {
            id = this.id;
            name = this.name;
            bytes = this.bytes;
            bundleId = this.bundleId;
            bundleBytes = this.bundleBytes;
            bundle2Id = this.bundle2Id;
        }

        public static implicit operator (int id, string name, byte[] bytes, int bundleId, byte[] bundleBytes, int bundle2Id)(SimTypeAsset value)
        {
            return (value.id, value.name, value.bytes, value.bundleId, value.bundleBytes, value.bundle2Id);
        }

        public static implicit operator SimTypeAsset((int id, string name, byte[] bytes, int bundleId, byte[] bundleBytes, int bundle2Id) value)
        {
            return new SimTypeAsset(value.id, value.name, value.bytes, value.bundleId, value.bundleBytes, value.bundle2Id);
        }
    }
}
