using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Wabbajack.Compression.BSA.Interfaces;
using Wabbajack.DTOs.BSA.ArchiveStates;
using Wabbajack.DTOs.Streams;

namespace Wabbajack.Compression.BSA.FO4Archive
{
  public class Reader : IReader
    {
        private Stream _stream;
        internal BinaryReader _rdr;
        internal uint _version;
        internal string _headerMagic;
        internal BA2EntryType _type;
        internal uint _numFiles;
        internal ulong _nameTableOffset;
        public IStreamFactory _streamFactory;
        public bool UseATIFourCC { get; set; } = false;

        public bool HasNameTable => _nameTableOffset > 0;

        
        
        public static async Task<Reader> Load(IStreamFactory streamFactory)
        {
            var rdr = new Reader(await streamFactory.GetStream()) {_streamFactory = streamFactory};
            await rdr.LoadHeaders();
            return rdr;
        }

        private Reader(Stream stream)
        {
            _stream = stream;
            _rdr = new BinaryReader(_stream, Encoding.UTF8);
        }

        private async Task LoadHeaders()
        {
            _headerMagic = Encoding.ASCII.GetString(_rdr.ReadBytes(4));

            if (_headerMagic != "BTDX")
                throw new InvalidDataException("Unknown header type: " + _headerMagic);

            _version = _rdr.ReadUInt32();
            
            string fourcc = Encoding.ASCII.GetString(_rdr.ReadBytes(4));

            if (Enum.TryParse(fourcc, out BA2EntryType entryType))
            {
                _type = entryType;
            }
            else
            {
                throw new InvalidDataException($"Can't parse entry types of {fourcc}");
            }

            _numFiles = _rdr.ReadUInt32();
            _nameTableOffset = _rdr.ReadUInt64();

            var files = new List<IBA2FileEntry>();
            for (var idx = 0; idx < _numFiles; idx += 1)
            {
                switch (_type)
                {
                    case BA2EntryType.GNRL:
                        files.Add(new FileEntry(this, idx));
                        break;
                    case BA2EntryType.DX10:
                        files.Add(new DX10Entry(this, idx));
                        break;
                    case BA2EntryType.GNMF:
                        break;

                }
            }

            if (HasNameTable)
            {
                _rdr.BaseStream.Seek((long) _nameTableOffset, SeekOrigin.Begin);
                foreach (var file in files)
                    file.FullPath = Encoding.UTF8.GetString(_rdr.ReadBytes(_rdr.ReadInt16()));
            }
            Files = files;
            _stream?.Dispose();
            _rdr.Dispose();

        }

        public IEnumerable<IFile> Files { get; private set; }
        
        public IArchive State => new BA2State
        {
            Version = _version,
            HeaderMagic = _headerMagic,
            Type = _type,
            HasNameTable = HasNameTable,
        };

    }
}