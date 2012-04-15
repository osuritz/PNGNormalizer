using System;
using System.IO;

namespace PNGNormalizer
{
    /// <summary>
    /// Similar to <see cref="BinaryWriter"/> but with the ability to specify the <see cref="Endianness"/>.
    /// </summary>
    internal class EndianBinaryWriter : IDisposable
    {
        private bool _disposed;
        private readonly Stream _stream;
        private readonly EndianBitConverter _bitConverter;

        /// <summary>
        /// Buffer used for temporary storage during conversion from primitives
        /// </summary>
        private readonly byte[] _buffer = new byte[16];

        /// <summary>
        /// Initializes a new instance of the <see cref="EndianBinaryWriter"/> class.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="bitConverter">The bit converter.</param>
        public EndianBinaryWriter(Stream output, EndianBitConverter bitConverter)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }

            if (bitConverter == null)
            {
                throw new ArgumentNullException("bitConverter");
            }

            if (!output.CanWrite)
            {
                throw new ArgumentException("Stream isn't writable", "output");
            }

            this._stream = output;
            this._bitConverter = bitConverter;
        }

        /// <summary>
        /// Gets the bit converter used to write values to the stream.
        /// </summary>
        public EndianBitConverter BitConverter { get { return this._bitConverter; } }

        /// <summary>
        /// Gets the underlying stream of the EndianBinaryWriter.
        /// </summary>
        /// <returns>The underlying stream associated with the EndianBinaryWriter.</returns>
        public virtual Stream BaseStream { get { return this._stream; } }


        /// <summary>
        /// Closes the writer, including the udnerlying stream.
        /// </summary>
        public void Close()
        {
            this.Dispose();
        }

        /// <summary>
        /// Clears all buffers for the current writer and causes any buffered data to
        /// be written to the underlying device.
        /// </summary>
        public virtual void Flush()
        {
            this.CheckDisposed();
            this._stream.Flush();
        }

        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
        /// <param name="origin">A value of type <see cref="System.IO.SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
        public void Seek(long offset, SeekOrigin origin)
        {
            CheckDisposed();
            this.BaseStream.Seek(offset, origin);
        }

        /// <summary>
        /// Writes a boolean value to the stream. 1 byte is written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(bool value)
        {
            BitConverter.CopyBytes(value, _buffer, 0);
            WriteInternal(_buffer, 1);
        }

        /// <summary>
        /// Writes a 16-bit signed integer to the stream, using the bit converter
        /// for this writer. 2 bytes are written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(short value)
        {
            BitConverter.CopyBytes(value, _buffer, 0);
            WriteInternal(_buffer, 2);
        }

        /// <summary>
        /// Writes a 32-bit signed integer to the stream, using the bit converter
        /// for this writer. 4 bytes are written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(int value)
        {
            BitConverter.CopyBytes(value, _buffer, 0);
            WriteInternal(_buffer, 4);
        }

        /// <summary>
        /// Writes a 64-bit signed integer to the stream, using the bit converter
        /// for this writer. 8 bytes are written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(long value)
        {
            BitConverter.CopyBytes(value, _buffer, 0);
            WriteInternal(_buffer, 8);
        }

        /// <summary>
        /// Writes a 16-bit unsigned integer to the stream, using the bit converter
        /// for this writer. 2 bytes are written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(ushort value)
        {
            BitConverter.CopyBytes(value, _buffer, 0);
            WriteInternal(_buffer, 2);
        }

        /// <summary>
        /// Writes a 32-bit unsigned integer to the stream, using the bit converter
        /// for this writer. 4 bytes are written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(uint value)
        {
            BitConverter.CopyBytes(value, _buffer, 0);
            WriteInternal(_buffer, 4);
        }
        
        /// <summary>
        /// Writes a 64-bit unsigned integer to the stream, using the bit converter
        /// for this writer. 8 bytes are written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(ulong value)
        {
            BitConverter.CopyBytes(value, _buffer, 0);
            WriteInternal(_buffer, 8);
        }

        /// <summary>
        /// Writes a single-precision floating-point value to the stream, using the bit converter
        /// for this writer. 4 bytes are written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(float value)
        {
            BitConverter.CopyBytes(value, _buffer, 0);
            WriteInternal(_buffer, 4);
        }

        /// <summary>
        /// Writes a double-precision floating-point value to the stream, using the bit converter
        /// for this writer. 8 bytes are written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(double value)
        {
            BitConverter.CopyBytes(value, _buffer, 0);
            WriteInternal(_buffer, 8);
        }

        /// <summary>
        /// Writes a decimal value to the stream, using the bit converter for this writer.
        /// 16 bytes are written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(decimal value)
        {
            BitConverter.CopyBytes(value, _buffer, 0);
            WriteInternal(_buffer, 16);
        }

        /// <summary>
        /// Writes a signed byte to the stream.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(byte value)
        {
            _buffer[0] = value;
            WriteInternal(_buffer, 1);
        }

        /// <summary>
        /// Writes an unsigned byte to the stream.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(sbyte value)
        {
            _buffer[0] = unchecked((byte)value);
            WriteInternal(_buffer, 1);
        }

        /// <summary>
        /// Writes an array of bytes to the stream.
        /// </summary>
        /// <param name="value">The values to write</param>
        public void Write(byte[] value)
        {
            if (value == null)
            {
                throw (new ArgumentNullException("value"));
            }

            WriteInternal(value, value.Length);
        }

        /// <summary>
        /// Writes a portion of an array of bytes to the stream.
        /// </summary>
        /// <param name="value">An array containing the bytes to write</param>
        /// <param name="offset">The index of the first byte to write within the array</param>
        /// <param name="count">The number of bytes to write</param>
        public void Write(byte[] value, int offset, int count)
        {
            CheckDisposed();
            this._stream.Write(value, offset, count);
        }

        #region Private methods

        /// <summary>
        /// Checks whether the writer has been disposed, throwing an exception if so.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        private void CheckDisposed()
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException("EndianBinaryWriter");
            }
        }

        /// <summary>
        /// Writes the specified number of bytes from the start of the given byte array,
        /// after checking whether or not the writer has been disposed.
        /// </summary>
        /// <param name="bytes">The array of bytes to write from</param>
        /// <param name="length">The number of bytes to write</param>
        private void WriteInternal(byte[] bytes, int length)
        {
            CheckDisposed();
            this._stream.Write(bytes, 0, length);
        }

        #endregion

        #region IDisposable members
        public void Dispose()
        {
            if (!this._disposed)
            {
                this.Flush();
                this._disposed = true;
                ((IDisposable)_stream).Dispose();
            }
        }
        #endregion
    }
}
