//
// BEncodedString.cs
//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2006 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//



using MonoTorrent.Client.Messages;
using MonoTorrent.Common;
using System;
using System.Text;

namespace MonoTorrent.BEncoding
{
    /// <summary>
    /// Class representing a BEncoded string
    /// </summary>
    public class BEncodedString : BEncodedValue, IComparable<BEncodedString>
    {
        #region Member Variables

        /// <summary>
        /// The value of the BEncodedString
        /// </summary>
        public string Text
        {
            get { return Encoding.UTF8.GetString(TextBytes); }
            set { TextBytes = Encoding.UTF8.GetBytes(value); }
        }

        /// <summary>
        /// The underlying byte[] associated with this BEncodedString
        /// </summary>
        public byte[] TextBytes { get; private set; }

        #endregion


        #region Constructors
        /// <summary>
        /// Create a new BEncodedString using UTF8 encoding
        /// </summary>
        public BEncodedString()
            : this(new byte[0])
        {
        }

        /// <summary>
        /// Create a new BEncodedString using UTF8 encoding
        /// </summary>
        /// <param name="value"></param>
        public BEncodedString(char[] value)
            : this(System.Text.Encoding.UTF8.GetBytes(value))
        {
        }

        /// <summary>
        /// Create a new BEncodedString using UTF8 encoding
        /// </summary>
        /// <param name="value">Initial value for the string</param>
        public BEncodedString(string value)
            : this(System.Text.Encoding.UTF8.GetBytes(value))
        {
        }


        /// <summary>
        /// Create a new BEncodedString using UTF8 encoding
        /// </summary>
        /// <param name="value"></param>
        public BEncodedString(byte[] value)
        {
            this.TextBytes = value;
        }


        public static implicit operator BEncodedString(string value)
        {
            return new BEncodedString(value);
        }
        public static implicit operator BEncodedString(char[] value)
        {
            return new BEncodedString(value);
        }
        public static implicit operator BEncodedString(byte[] value)
        {
            return new BEncodedString(value);
        }
        #endregion


        #region Encode/Decode Methods


        /// <summary>
        /// Encodes the BEncodedString to a byte[] using the supplied Encoding
        /// </summary>
        /// <param name="buffer">The buffer to encode the string to</param>
        /// <param name="offset">The offset at which to save the data to</param>
        /// <param name="e">The encoding to use</param>
        /// <returns>The number of bytes encoded</returns>
        public override int Encode(byte[] buffer, int offset)
        {
            int written = offset;
            written += WriteLengthAsAscii(buffer, written, TextBytes.Length);
            written += Message.WriteAscii(buffer, written, ":");
            written += Message.Write(buffer, written, TextBytes);
            return written - offset;
        }

        int WriteLengthAsAscii (byte[] buffer, int offset, int asciiLength)
        {
            if (asciiLength > 100000)
                return Message.WriteAscii(buffer, offset, TextBytes.Length.ToString());

            bool hasWritten = false;
            int written = offset;
            for (int remainder = 100000; remainder > 1; remainder /= 10)
            {
                if (asciiLength < remainder && !hasWritten)
                    continue;
                byte resultChar = (byte)('0' + asciiLength / remainder);
                written += Message.Write(buffer, written, resultChar);
                asciiLength %= remainder;
                hasWritten = true;
            }
            written += Message.Write (buffer, written, (byte)('0' + asciiLength));
            return written - offset;
        }


        /// <summary>
        /// Decodes a BEncodedString from the supplied StreamReader
        /// </summary>
        /// <param name="reader">The StreamReader containing the BEncodedString</param>
        internal override void DecodeInternal(RawReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            int letterCount;
            string length = string.Empty;

            while ((reader.PeekByte() != -1) && (reader.PeekByte() != ':'))         // read in how many characters
            {
                length += (char)reader.ReadByte();                                 // the string is
            }

            if (reader.ReadByte() != ':')                                           // remove the ':'
            {
                throw new BEncodingException("Invalid data found. Aborting");
            }

            if (!int.TryParse(length, out letterCount))
            {
                throw new BEncodingException(string.Format("Invalid BEncodedString. Length was '{0}' instead of a number", length));
            }

            this.TextBytes = new byte[letterCount];
            if (reader.Read(TextBytes, 0, letterCount) != letterCount)
            {
                throw new BEncodingException("Couldn't decode string");
            }
        }
        #endregion


        #region Helper Methods
        public string Hex
        {
            get { return BitConverter.ToString(TextBytes); }
        }

        public override int LengthInBytes()
        {
            // The length is equal to the length-prefix + ':' + length of data
            int prefix = 1; // Account for ':'

            // Count the number of characters needed for the length prefix
            for (int i = TextBytes.Length; i != 0; i = i / 10)
            {
                prefix += 1;
            }

            if (TextBytes.Length == 0)
            {
                prefix++;
            }

            return prefix + TextBytes.Length;
        }

        public int CompareTo(object other)
        {
            return CompareTo(other as BEncodedString);
        }


        public int CompareTo(BEncodedString other)
        {
            if (other == null)
            {
                return 1;
            }

            int difference = 0;
            int length = this.TextBytes.Length > other.TextBytes.Length ? other.TextBytes.Length : this.TextBytes.Length;

            for (int i = 0; i < length; i++)
            {
                if ((difference = this.TextBytes[i].CompareTo(other.TextBytes[i])) != 0)
                {
                    return difference;
                }
            }

            if (this.TextBytes.Length == other.TextBytes.Length)
            {
                return 0;
            }

            return this.TextBytes.Length > other.TextBytes.Length ? 1 : -1;
        }

        #endregion


        #region Overridden Methods

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            BEncodedString other;
            if (obj is string)
            {
                other = new BEncodedString((string)obj);
            }
            else if (obj is BEncodedString)
            {
                other = (BEncodedString)obj;
            }
            else
            {
                return false;
            }

            return Toolbox.ByteMatch(this.TextBytes, other.TextBytes);
        }

        public override int GetHashCode()
        {
            int hash = 0;
            for (int i = 0; i < this.TextBytes.Length; i++)
            {
                hash += this.TextBytes[i];
            }

            return hash;
        }

        public override string ToString()
        {
            return System.Text.Encoding.UTF8.GetString(TextBytes);
        }

        #endregion
    }
}
