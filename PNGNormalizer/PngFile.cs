/*
 * PNGNormalizer - iOS PNG Image Normalizer 1.0
 * Copyright (C) 2012
 * 
 * Author
 *   Olivier Suritz
 *   olivier.suritz@gmail.com
 * 
 * Provides PNG normalization for files that have been crushed,
 * especially PNGs optimized for iOS with Apple's version of pngcrush
 * 
 * References
 * - CgBI file format info at http://iphonedevwiki.net/index.php/CgBI_file_format
 * - Logic inspired by https://github.com/pcans/PngCompote/blob/master/pngCompote.php
 * - PNG Image File Format documentation found at http://www.fileformat.info/format/png/corion.htm
 * 
 * PNGNormalizer is free software and it's released under the Apache License, Version 2.0 (the "License");
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 */

using System;
using System.IO;
using System.Text;
using Ionic.Zlib;

namespace PNGNormalizer
{
    /// <summary>
    /// Reads PNG image files, including Apple-optimized, crushed PNG files used on iOS devies.
    /// </summary>
    public class PngFile
    {
        private const string ChunkTypeCgbi = "CgBI";
        private const string ChunkTypeImageHeader = "IHDR";
        private const string ChunkTypeImageData = "IDAT";
        private const string ChunkTypeImageTrailer = "IEND";
        
        internal static readonly UInt64 PngHeader = BitConverter.ToUInt64(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A }, 0);
        internal const UInt64 PngHeaderBigEndian = 0x89504E470D0A1A0A;
        internal const UInt64 PngHeaderLittleEndian = 0x0A1A0A0D474E5089;

        /// <summary>
        /// Initializes a new instance of the <see cref="PngFile"/> class.
        /// </summary>
        /// <param name="data">The PNG data.</param>
        public PngFile(byte[] data)
        {
            using (var readerStream = new MemoryStream(data, false))
            {
                var binaryReader = new BinaryReader(readerStream);

                // Read and check for PNG header (8 bytes).
                ulong header = binaryReader.ReadUInt64();
                if (header != PngHeader)
                {
                    throw new ArgumentException("It is NOT a PNG file.", "data");
                }

                // Check the first chunk header for CgBI (an extra critical header indicative of Apple's crunshed PNG format)
                // http://iphonedevwiki.net/index.php/CgBI_file_format
                var chunkHeader = new PngChunkHeader(binaryReader);

                if (!chunkHeader.IsChunkType(ChunkTypeCgbi))
                {
                    // No conversion needed.
                    this.Data = data;
                    return;
                }

                this.IsIphoneCrushed = true;

                // Need to "rewind" by the size of the chunk header
                readerStream.Seek(PngChunkHeader.ChunkHeaderSize * -1, SeekOrigin.Current);

                this.UncrushPngData(readerStream, binaryReader);
            }
        }

        #region Properties

        /// <summary>
        /// Gets or sets the normalized data.
        /// </summary>
        /// <value>
        /// The data.
        /// </value>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the original data is iOS (iPhone/iPad/etc.) crushed (CgBI).
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the original data is iOS crushed; otherwise, <c>false</c>.
        /// </value>
        public bool IsIphoneCrushed { get; private set; }

        /// <summary>
        /// Gets the image width.
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// Gets the image height.
        /// </summary>
        public int Height { get; private set; }

        #endregion

        private void UncrushPngData(Stream readerStream, BinaryReader binaryReader)
        {
            // Create new, uncrushed PNG data
            byte[] newPngData = null;

            using (var writerStream = new MemoryStream())
            {
                //var writer = new BinaryWriter(writerStream);
                var writer = new EndianBinaryWriter(writerStream, EndianBitConverter.BigEndianConverter);

                // Write header
                writer.Write(PngHeaderBigEndian);

                while (readerStream.Position < readerStream.Length - 1)
                {
                    // Read next chunk
                    var chunk = new PngChunk(binaryReader);

                    // Skip the CgBI chunk.
                    if (chunk.Header.IsChunkType(ChunkTypeCgbi))
                    {
                        continue;
                    }

                    // Parse the header chunk
                    if (chunk.Header.IsChunkType(ChunkTypeImageHeader))
                    {
                        uint width = chunk.Data.ReadBigEndianUInt32();
                        uint height = chunk.Data.ReadBigEndianUInt32(startIndex: sizeof (UInt32));

                        this.Width = (int) width;
                        this.Height = (int) height;
                    } else if (chunk.Header.IsChunkType(ChunkTypeImageData))
                    {
                        // Uncompress (inflate) the image data chunk.
                        byte[] inflatedData = Decompress(chunk.Data);

                        /* Note:
                         * -----
                         * In CgBI, pixel data are byteswapped (RGBA -> BGRA),
                         * presumably for high-speed direct blitting to the frame buffer.
                         */

                        // Swap red & blue bytes for each pixel (to switch back to RGBA).
                        var scanLineSize = 1 + (this.Width * 4);   // Line length = filtertype + nb pixel on a line * size of a pixel
                        byte[] newData;
                        using (var dataRes = new MemoryStream())
                        {
                            for (int y = 0; y < this.Height; y++)
                            {
                                // Filter-type
                                var filterType = inflatedData[y * scanLineSize];
                                dataRes.WriteByte(filterType);

                                for (int x = 0; x < this.Width; x++)
                                {
                                    var pixel = inflatedData.Take(4, (y * scanLineSize + 1) + (x * 4));
                                    dataRes.WriteBytes(pixel[2], pixel[1], pixel[0], pixel[3]);                                                                        
                                }
                            }

                            newData = dataRes.ToArray();
                        }

                        // Compress (gzip) the image chunk.
                        byte[] deflatedData = Compress(newData);

                        // Update dat length on chunk header
                        chunk.Header.DataLength = (uint) deflatedData.Length;

                        chunk.Data = deflatedData;

                        // Update CRC
                        chunk.RecomputeCrc();
                    }

                    chunk.WriteTo(writer);
                    newPngData = writerStream.ToArray();

                    // Stop parsing the PNG file.
                    if (chunk.Header.IsChunkType(ChunkTypeImageTrailer))
                    {                        
                        break;
                    }
                }
            }

            this.Data = newPngData;
        }


        /// <summary>
        /// Compresses the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="compressionLevel">The compression level.</param>
        /// <returns></returns>
        private static byte[] Compress(byte[] data, CompressionLevel compressionLevel = CompressionLevel.Default)
        {
            // While the CgBI PNG image data is compressed with Deflate, a "real" PNG must use Zlib.
            using (var output = new MemoryStream())
            {
                using (var deflateStream = new ZlibStream(output, CompressionMode.Compress, compressionLevel, true))
                {
                    deflateStream.Write(data, 0, data.Length);
                }

                return output.ToArray();
            }
        }

        private static byte[] Decompress(byte[] data)
        {
            // While a "real" PNG uses Zlib for the image data, the CgBI PNG image data is compressed with Deflate.
            using (var inputStream = new MemoryStream(data, false))
            {
                using (var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress))
                {
                    using (var newData = new MemoryStream())
                    {
                        deflateStream.CopyTo(newData);
                        return newData.ToArray();
                    }
                }            
                
            }
        }

        /// <summary>
        /// A PNG chunk header is made of 8 bytes. The first 4 indicate the length of the chunk data.
        /// The last 4 indicate the chunk type.
        /// </summary>
        private class PngChunkHeader
        {
            internal const int ChunkHeaderSize = ChunkTypeLength + sizeof(UInt32);
            private const int ChunkTypeLength = 4;

            /// <summary>
            /// Initializes a new instance of the <see cref="PngChunkHeader"/> class.
            /// </summary>
            /// <param name="reader">The reader.</param>
            public PngChunkHeader(BinaryReader reader)
            {
                this.DataLength = reader.ReadBigEndianUInt32();

                char[] chunkType = reader.ReadChars(ChunkTypeLength);
                this.ChunkType = new string(chunkType);
            }

            public uint DataLength { get; set; }

            /// <summary>
            /// Gets the chunk type.
            /// </summary>
            /// <value>
            /// The type of the chunk.
            /// </value>
            public string ChunkType { get; private set; }

            public bool IsChunkType(string chunkType)
            {
                bool match = string.Equals(chunkType, this.ChunkType, StringComparison.Ordinal);
                return match;
            }
        }

        /// <summary>
        /// A PNG chunk is made of a chunk header (8 bytes), its data, and a CRC.
        /// </summary>
        private class PngChunk
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PngChunk"/> class.
            /// </summary>
            /// <param name="reader">The reader.</param>
            public PngChunk(BinaryReader reader)
            {
                this.Header = new PngChunkHeader(reader);
                this.Data = reader.ReadBytes((int)this.Header.DataLength);
                this.Crc = reader.ReadUInt32();
            }

            public PngChunkHeader Header { get; private set; }
            public byte[] Data { get; set; }
            private UInt32 Crc { get; set; }

            /// <summary>
            /// Writes the chunk to the specified <see cref="BinaryWriter"/>.
            /// </summary>
            /// <param name="writer">The writer.</param>
            public void WriteTo(EndianBinaryWriter writer)
            {
                writer.Write(this.Data.Length);
                writer.Write(Encoding.ASCII.GetBytes(this.Header.ChunkType));
                writer.Write(this.Data);
                byte[] crc = BitConverter.GetBytes(this.Crc);
                writer.Write(crc);
            }

            /// <summary>
            /// Recomputes the Cyclyic Redundancy Check (CRC) for the current chunk.
            /// </summary>
            public void RecomputeCrc()
            {
                /* CRC is calculated on the preceding bytes in that chunk,
                 * including the chunk type code and chunk data fields,
                 * but not including the length field.
                 */
                
                uint crc = Crc32.ComputeCrc32(Encoding.ASCII.GetBytes(this.Header.ChunkType));
                crc = Crc32.ComputeCrc32(this.Data, crc);
                crc = (uint) ((crc + 0x100000000) % 0x100000000);

                this.Crc = crc;
            }
        }
    }
}