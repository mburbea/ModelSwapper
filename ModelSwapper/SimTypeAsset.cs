using System;
using System.Collections.Generic;

namespace ModelSwapper
{
    internal struct SimTypeAsset
    {
        internal static byte[] Bundle2Bytes = { 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0x45, 0xAB, 0x1A, 0, 0, 0, 0, };
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] Bytes;
        public int BundleId => unchecked((int)0xF0_00_00_00) | Id;
        public byte[] BundleBytes;
        public int Bundle2Id => 0x20_00_00_00 | Id;

        public SimTypeAsset(int id, string name, byte[] bytes, byte[] bundleBytes)
        {
            Id = id;
            Name = name;
            Bytes = bytes;
            BundleBytes = bundleBytes;
        }

        public override bool Equals(object obj)
        {
            return obj is SimTypeAsset other &&
                   Id == other.Id &&
                   Name == other.Name &&
                   EqualityComparer<byte[]>.Default.Equals(Bytes, other.Bytes) &&
                   BundleId == other.BundleId &&
                   EqualityComparer<byte[]>.Default.Equals(BundleBytes, other.BundleBytes) &&
                   Bundle2Id == other.Bundle2Id;
        }


        public override int GetHashCode() => (Id, Name).GetHashCode();
    }
}
