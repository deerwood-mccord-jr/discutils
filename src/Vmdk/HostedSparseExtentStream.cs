﻿//
// Copyright (c) 2008-2009, Kenneth Bell
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.IO;

namespace DiscUtils.Vmdk
{
    /// <summary>
    /// Represents and extent from a sparse disk from 'hosted' software (VMWare Workstation, etc).
    /// </summary>
    /// <remarks>Hosted disks and server disks (ESX, etc) are subtly different formats.</remarks>
    internal class HostedSparseExtentStream : SparseStream
    {
        /// <summary>
        /// Stream containing the sparse extent.
        /// </summary>
        private Stream _fileStream;

        /// <summary>
        /// Indicates if this object controls the lifetime of _fileStream.
        /// </summary>
        private bool _ownsFileStream;

        /// <summary>
        /// Offset of this extent within the disk.
        /// </summary>
        private long _diskOffset;

        /// <summary>
        /// The stream containing the unstored bytes.
        /// </summary>
        private Stream _parentDiskStream;

        /// <summary>
        /// Indicates if this object controls the lifetime of _parentDiskStream.
        /// </summary>
        private bool _ownsParentDiskStream;

        /// <summary>
        /// The Global Directory for this extent.
        /// </summary>
        private uint[] _globalDirectory;

        /// <summary>
        /// The header from the start of the extent.
        /// </summary>
        private SparseExtentHeader _header;

        /// <summary>
        /// The number of bytes controlled by a single grain table.
        /// </summary>
        private long _gtCoverage;

        /// <summary>
        /// The current grain that's loaded into _grainTable.
        /// </summary>
        private int _currentGrain;

        /// <summary>
        /// The data corresponding to the current grain (or null).
        /// </summary>
        private uint[] _grainTable;

        /// <summary>
        /// Current position in the extent.
        /// </summary>
        private long _position;

        public HostedSparseExtentStream(Stream file, bool ownsFile, long diskOffset, Stream parentDiskStream, bool ownsParentDiskStream)
        {
            _fileStream = file;
            _ownsFileStream = ownsFile;
            _diskOffset = diskOffset;
            _parentDiskStream = parentDiskStream;
            _ownsParentDiskStream = ownsParentDiskStream;

            file.Position = 0;
            byte[] firstSector = Utilities.ReadFully(file, Sizes.Sector);
            _header = SparseExtentHeader.Read(firstSector, 0);

            _gtCoverage = _header.NumGTEsPerGT * _header.GrainSize * Sizes.Sector;

            LoadGlobalDirectory();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (_ownsFileStream && _fileStream != null)
                    {
                        _fileStream.Dispose();
                        _fileStream = null;
                    }

                    if (_ownsParentDiskStream && _parentDiskStream != null)
                    {
                        _parentDiskStream.Dispose();
                        _parentDiskStream = null;
                    }
                }
            }
            catch
            {
                base.Dispose(disposing);
            }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return _fileStream.CanWrite; }
        }

        public override void Flush()
        {
            
        }

        public override long Length
        {
            get { return _header.Capacity * Sizes.Sector; }
        }

        public override long Position
        {
            get { return _position; }
            set { _position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int grainTable = (int)(_position / _gtCoverage);
            int grainTableOffset = (int)(_position - (grainTable * _gtCoverage));

            LoadGrainTable(grainTable);

            int grainSize = (int)(_header.GrainSize * Sizes.Sector);
            int grain = grainTableOffset / grainSize;
            int grainOffset = grainTableOffset - (grain * grainSize);

            int numToRead = Math.Min(count, grainSize - grainOffset);
            _fileStream.Position = (_grainTable[grain] * Sizes.Sector) + grainOffset;
            int numRead = _fileStream.Read(buffer, offset, numToRead);

            _position += numRead;
            return numRead;
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<StreamExtent> Extents
        {
            get { throw new NotImplementedException(); }
        }

        private void LoadGlobalDirectory()
        {
            int numGTs = (int)Utilities.Ceil(_header.Capacity * Sizes.Sector, _gtCoverage);
            _globalDirectory = new uint[numGTs];
            _fileStream.Position = _header.GdOffset * Sizes.Sector;
            byte[] gdAsBytes = Utilities.ReadFully(_fileStream, numGTs * 4);
            for (int i = 0; i < _globalDirectory.Length; ++i)
            {
                _globalDirectory[i] = Utilities.ToUInt32LittleEndian(gdAsBytes, i * 4);
            }
        }

        private void LoadGrainTable(int index)
        {
            if (_grainTable != null && _currentGrain == index)
            {
                return;
            }

            uint[] newGrainTable = _grainTable;
            _grainTable = null;
            if (newGrainTable == null)
            {
                newGrainTable = new uint[_header.NumGTEsPerGT];
            }

            _fileStream.Position = _globalDirectory[index] * Sizes.Sector;
            byte[] buffer = Utilities.ReadFully(_fileStream, (int)(_header.NumGTEsPerGT * 4));

            for (int i = 0; i < _header.NumGTEsPerGT; ++i)
            {
                newGrainTable[i] = Utilities.ToUInt32LittleEndian(buffer, i * 4);
            }

            _currentGrain = index;
            _grainTable = newGrainTable;
        }

    }
}