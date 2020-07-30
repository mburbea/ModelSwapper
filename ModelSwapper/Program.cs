using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;

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
            return WriteSimtypeMgrInternal(new[] { unchecked((int)0x80_00_1F_C2u), unchecked((int)0x80_1d_80_74u) }.Concat(entries).ToArray());
        }

        static SimTypeAsset CreateModifiedSimType(Fab fab, string maleName, string femaleName, string baseSimName)
        {
            var bytes = File.ReadAllBytes($"{fab}.simtype_bxml");
            ReadOnlySpan<byte> target = new byte[8] { 0x07, 0, 0, 0, 0, 0, 0, 0 };
            var ix = bytes.AsSpan().IndexOf(target);
            bool modified = false;
            if (FabDictByName.TryGetValue($"{maleName}{fab}", out uint maleId))
            {
                modified = true;
                bytes.Write(ix + 9, maleId);
            }
            if (FabDictByName.TryGetValue($"{femaleName}{fab}", out uint femaleId))
            {
                modified = true;
                bytes.Write(ix + 13, femaleId);
            }
            maleId = maleId > 0 ? maleId : bytes.Read<uint>(ix + 9);
            femaleId = femaleId > 0 ? femaleId : bytes.Read<uint>(ix + 13);
            if (modified)
            {
                var name = $"{baseSimName}{fab}";
                var id = Hasher.GetHash(name);
                var bundleBytes = new byte[30];
                bundleBytes.Write(8, 2);
                bundleBytes.Write(16, maleId);
                bundleBytes.Write(20, femaleId);
                bundleBytes.Write(24, 0x00_00_10_10);
                return new SimTypeAsset(id, name, bytes, bundleBytes);
            }
            return default;
        }

        static IEnumerable<SimTypeAsset> BuildTable(string maleName, string femaleName, string baseSimType = "Clothing_peasant03_")
        => ((Fab[])typeof(Fab).GetEnumValues())
                .Select(x => CreateModifiedSimType(x, maleName, femaleName, baseSimType))
                .Where(x => x.Bytes != null);

        static byte[] BuildSimtypeInit(IEnumerable<int> newEntries)
        {
            var newIds = newEntries.ToArray();
            ReadOnlySpan<byte> data = File.ReadAllBytes("simtype_init.bin");
            var newData = new byte[data.Length + newIds.Length * 8];
            var currentLen = data.Read<int>(4);
            var listSize = currentLen * 4;
            var eol1 = listSize + 8;
            var eol2 = eol1 + newIds.Length * 4 + listSize;
            data.Slice(0, eol1).CopyTo(newData);
            data.Slice(eol1).CopyTo(newData.AsSpan(eol1 + newIds.Length * 4));
            newData.Write(4, currentLen + newIds.Length);

            foreach (var id in newIds)
            {
                newData.Write(eol1, id);
                newData.Write(eol2, id);
                eol1 += 4;
                eol2 += 4;
            }
            return newData;
        }

        private static void Main()
        {
            const string bundlePath = @"output\bigs\001\BundleTarget\generated_patch.big";
            const string patchPath = @"output\bigs\001\Patches\zpatch.big";
            FabDictByName.Add("01_generic_peasant_female_feet", FabDictByName["01_generic_female_peasant_feet"]);

            var simtypeTable = new[] 
            {
                BuildTable("splinter_02_melee_Set_Unique_", "splinter_02_melee_Set_Unique_f_","glowing_warrior_"),
                BuildTable("splinter_03_Rogue_Set_Unique_", "splinter_03_Rogue_Set_Unique_f_", "glowing_rogue_"),
                BuildTable("01_generic_male_peasant_", "01_generic_peasant_female_", "Clothing_peasant03_"),
                BuildTable("01_Dokkalfar_noble_male_", "01_Dokkalfar_noble_female_", "Clothing_peasant04_"),
                BuildTable("02_Dokkalfar_noble_male_", "02_Dokkalfar_noble_female_", "Clothing_peasant05_"),
                BuildTable("01_Dokkalfar_peasant_male_", "01_Dokkalfar_peasant_female_", "Clothing_peasant06_"),
                BuildTable("02_Dokkalfar_peasant_male_", "02_Dokkalfar_peasant_female_", "Clothing_peasant07_"),
                BuildTable("01_Dokkalfar_merchant_male_", "01_Dokkalfar_merchant_female_", "Clothing_peasant08_"),
                BuildTable("02_Dokkalfar_merchant_male_", "02_Dokkalfar_merchant_female_", "Clothing_peasant09_"),

                BuildTable("01_Ljosalfar_noble_male_", "01_Ljosalfar_noble_female_", "Clothing_peasant10_"),
                BuildTable("02_Ljosalfar_noble_male_", "02_Ljosalfar_noble_female_", "Clothing_peasant11_"),
                BuildTable("01_Ljosalfar_peasant_male_", "01_Ljosalfar_peasant_female_", "Clothing_peasant12_"),
                BuildTable("02_Ljosalfar_peasant_male_", "02_Ljosalfar_peasant_female_", "Clothing_peasant13_"),
                BuildTable("01_Ljosalfar_merchant_male_", "01_Ljosalfar_Merchant_f_", "Clothing_peasant14_"),
                BuildTable("02_Ljosalfar_merchant_male_", "02_Ljosalfar_Merchant_f_", "Clothing_peasant15_"),

                BuildTable("01_almain_merchant_male_", "01_almain_Merchant_female_", "Clothing_peasant16_"),
                BuildTable("02_almain_merchant_male_", "02_almain_Merchant_female_", "Clothing_peasant17_"),
                BuildTable("01_varani_merchant_male_", "01_varani_Merchant_female_", "Clothing_peasant18_"),
                BuildTable("02_varani_merchant_male_", "02_varani_Merchant_female_", "Clothing_peasant19_"),
                BuildTable("01_almain_noble_male_", "01_almain_noble_female_", "Clothing_peasant20_"),
                BuildTable("02_almain_noble_male_", "02_almain_noble_female_", "Clothing_peasant21_"),
                BuildTable("01_varani_noble_male_", "01_varani_noble_female_", "Clothing_peasant22_"),
                BuildTable("02_varani_noble_male_", "02_varani_noble_female_", "Clothing_peasant23_"),
            }.SelectMany(x => x)
             .ToArray();

            if (Directory.Exists("output"))
            {
                Directory.Delete(@"output", true);
            }
            Directory.CreateDirectory(@"output\_scripts\console\");
            Directory.CreateDirectory(@"output\bigs\001\BundleTarget\");
            Directory.CreateDirectory(@"output\bigs\001\Patches\");

            File.Copy("createclothes.lua", @"output\_scripts\console\createclothes.lua", true);

            var simtypeMgr = BuildSimtypeMgr(simtypeTable.Select(x => x.Id));
            File.WriteAllBytes(bundlePath, new BigFileBuilder 
            {
                { 0x_03_FF_6F_48, simtypeMgr, 0x14 },
                simtypeTable.SelectMany(x => new[] {
                    (x.BundleId, x.BundleBytes),
                    (x.Bundle2Id, SimTypeAsset.Bundle2Bytes)
                }) 
            }.Build());

            var simtypeInitFile = BuildSimtypeInit(simtypeTable.Select(x => x.Id));
            File.WriteAllBytes(patchPath, new BigFileBuilder 
            {
                 { 0x01_CA_82_FD, simtypeInitFile, 0x14 },
                simtypeTable.Select(x => (x.Id, x.Bytes, 0x94)) 
            }.Build());

            if (File.Exists("clothingmod.zip"))
            {
                File.Delete("clothingmod.zip");
            }
            ZipFile.CreateFromDirectory("output", "clothingmod.zip");
        }
    }
}
