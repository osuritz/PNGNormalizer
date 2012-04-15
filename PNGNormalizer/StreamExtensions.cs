using System;
using System.IO;
using System.Linq;

namespace PNGNormalizer
{
    /// <summary>
    /// Provides extension methods to <see cref="Stream"/>, byte arrays, and <see cref="BinaryReader"/>.
    /// </summary>
    public static class StreamExtensions
    {
        ////public static byte[] Read(this Stream stream, long length)
        ////{
        ////    var buffer = new byte[length];
        ////    int read = stream.Read(buffer, 0, (int)length);

        ////    if (read < length)
        ////    {
        ////        return buffer.Take(read).ToArray();
        ////    }

        ////    return buffer;
        ////}

        ////private static short ReadLittleEndianInt16(this BinaryReader binaryReader)
        ////{
        ////    var bytes = new byte[sizeof(short)];
        ////    for (int i = 0; i < sizeof(short); i += 1)
        ////    {
        ////        bytes[sizeof(short) - 1 - i] = binaryReader.ReadByte();
        ////    }
        ////    return BitConverter.ToInt16(bytes, 0);
        ////}
        
        ////public static int ReadLittleEndianInt32(this BinaryReader binaryReader)
        ////{
        ////    var bytes = new byte[sizeof(int)];
        ////    for (int i = 0; i < sizeof(int); i+=1)
        ////    {
        ////        bytes[sizeof(int) - 1 - i] = binaryReader.ReadByte();
        ////    }

        ////    return BitConverter.ToInt32(bytes, 0);
        ////}

        ////public static int ReadBigEndianInt32(this BinaryReader binaryReader)
        ////{
        ////    if (BitConverter.IsLittleEndian)
        ////    {
        ////        byte[] arr = binaryReader.ReadBytes(sizeof(UInt32));
        ////        int val = BitConverter.ToInt32(arr.Reverse().ToArray(), 0);
        ////        return val;
        ////    }

        ////    return binaryReader.ReadInt32();            
        ////}

        /// <summary>
        /// Reads a Big-Endian <see cref="uint"/> from the byte array.
        /// </summary>
        /// <param name="bytes">The byte array from which to read the UInt32.</param>
        /// <param name="startIndex">The zero-based starting position of the UInt32 in this instance.</param>
        /// <returns></returns>
        public static uint ReadBigEndianUInt32(this byte[] bytes, int startIndex = 0)
        {
            if (BitConverter.IsLittleEndian)
            {
                uint val = BitConverter.ToUInt32(bytes.Take(sizeof(UInt32)).Reverse().ToArray(), 0);
                return val;
            }

            return BitConverter.ToUInt32(bytes, startIndex);
        }

        /// <summary>
        /// Reads a Big-Endian <see cref="uint"/> from the byte array.
        /// </summary>
        /// <param name="binaryReader">The <see cref="BinaryReader"/> from which to read a UInt32</param>
        /// <returns></returns>
        public static uint ReadBigEndianUInt32(this BinaryReader binaryReader)
        {
            if (BitConverter.IsLittleEndian)
            {
                return binaryReader.ReadBytes(sizeof(UInt32)).ReadBigEndianUInt32();
            }

            return binaryReader.ReadUInt32();
        }

        /// <summary>
        /// Retrieves a subset array of bytes from this instance.
        /// </summary>
        /// <param name="source">The source array from which to .</param>
        /// <param name="length">The number of bytes in the subset array.</param>
        /// <param name="startOffset">The zero-based starting position of a subset array in this instance.</param>
        /// <returns></returns>
        public static byte[] Take(this byte[] source, int length, long startOffset = 0)
        {
            var bytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                bytes[i] = source[i + startOffset];
            }

            return bytes;
        }

        /// <summary>
        /// Writes an arbitrary number of bytes to the stream, instead of creating an for them or calling Write multiple times.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="args">A byte array that contains zero or more bytes to write to the stream.</param>
        public static void WriteBytes(this Stream stream, params byte[] args)
        {
            foreach (byte value in args)
            {
                stream.WriteByte(value);
            }
        }
    }
}