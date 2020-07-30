using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ModelSwapper
{
    public class BigFileBuilder : IEnumerable
    {
        private readonly List<(int id, byte[] data, int flag)> _files = new List<(int id, byte[] data, int flag)>();

        public void Add(int id, byte[] data, int flag = 0x94) => _files.Add((id, data, flag));

        public void Add(IEnumerable<(int id, byte[] data)> files, int flag = 0x94) => Add(files.Select(x => (x.id, x.data, flag)));

        public void Add(IEnumerable<(int id, byte[] data, int flag)> files) => _files.AddRange(files);

        public byte[] Build()
        {
            ReadOnlySpan<byte> header = new byte[] {
                0x80, 0xC7, 0xC8, 0xC2, 0x10, 0x00, 0x00, 0x00, 0x01, 0x1E, 0x00, 0x00, 0x00, 0x42, 0x48, 0x47,
                0x36, 0x31, 0x32, 0x30, 0x20, 0x3A, 0x20, 0x33, 0x38, 0x43, 0x4F, 0x52, 0x50, 0x20, 0x3A, 0x20,
                0x62, 0x68, 0x67, 0x2E, 0x62, 0x75, 0x69, 0x6C, 0x64, 0x65, 0x72, 0x20, 0x00, 0x00, 0x00 };
            var bytes = new byte[header.Length
                + 4 // length of entries
                + 20 * _files.Count // each entry has 20 bytes in the header.
                + _files.Sum(x => x.data.Length)];
            header.CopyTo(bytes);
            bytes.Write(header.Length, _files.Count);
            var currentOffset = header.Length + 4;
            var fileOffset = currentOffset + (20 * +_files.Count);
            foreach (var (id, data, flag) in _files)
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

        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    }
}
