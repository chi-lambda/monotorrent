//
// BEncodedNumber.cs
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



using System;
using System.Text;

namespace MonoTorrent.BEncoding
{
    /// <summary>
    /// Class representing a BEncoded number
    /// </summary>
    public class BEncodedNumber : BEncodedValue, IComparable<BEncodedNumber>
    {
        public long number;
        #region Member Variables
        /// <summary>
        /// The value of the BEncodedNumber
        /// </summary>
        public long Number { get => number; set => number = value; }
        #endregion


        #region Constructors
        public BEncodedNumber() : this(0) { }

        /// <summary>
        /// Create a new BEncoded number with the given value
        /// </summary>
        /// <param name="initialValue">The inital value of the BEncodedNumber</param>
        public BEncodedNumber(long value)
        {
            this.Number = value;
        }

        public static implicit operator BEncodedNumber(long value)
        {
            return new BEncodedNumber(value);
        }
        #endregion


        #region Encode/Decode Methods

        /// <summary>
        /// Encodes this number to the supplied byte[] starting at the supplied offset
        /// </summary>
        /// <param name="buffer">The buffer to write the data to</param>
        /// <param name="offset">The offset to start writing the data at</param>
        /// <returns></returns>
        public override int Encode(byte[] buffer, int offset)
        {
            var number = Encoding.ASCII.GetBytes(Number.ToString());

            int written = offset;
            buffer[written++] = (byte)'i';
            number.CopyTo(buffer, written);
            written += number.Length;

            buffer[written++] = (byte)'e';
            return written - offset;
        }


        /// <summary>
        /// Decodes a BEncoded number from the supplied RawReader
        /// </summary>
        /// <param name="reader">RawReader containing a BEncoded Number</param>
        internal override void DecodeInternal(RawReader reader)
        {
            int sign = 1;
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            if (reader.ReadByte() != 'i')              // remove the leading 'i'
            {
                throw new BEncodingException("Invalid data found. Aborting.");
            }

            if (reader.PeekByte() == '-')
            {
                sign = -1;
                reader.ReadByte();
            }

            int letter;
            while (((letter = reader.PeekByte()) != -1) && letter != 'e')
            {
                if (letter < '0' || letter > '9')
                {
                    throw new BEncodingException("Invalid number found.");
                }

                Number = Number * 10 + (letter - '0');
                reader.ReadByte();
            }
            if (reader.ReadByte() != 'e')        //remove the trailing 'e'
            {
                throw new BEncodingException("Invalid data found. Aborting.");
            }

            Number *= sign;
        }
        #endregion


        #region Helper Methods
        /// <summary>
        /// Returns the length of the encoded string in bytes
        /// </summary>
        /// <returns></returns>
        public override int LengthInBytes()
        {
            long number = this.Number;
            int count = 2; // account for the 'i' and 'e'

            if (number == 0)
            {
                return count + 1;
            }

            if (number < 0)
            {
                number = -number;
                count++;
            }
            for (long i = number; i != 0; i /= 10)
            {
                count++;
            }

            return count;
        }


        public int CompareTo(object other)
        {
            if (other is BEncodedNumber || other is long || other is int)
            {
                return CompareTo((BEncodedNumber)other);
            }

            return -1;
        }

        public int CompareTo(BEncodedNumber other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            return this.Number.CompareTo(other.Number);
        }


        public int CompareTo(long other)
        {
            return this.Number.CompareTo(other);
        }
        #endregion


        #region Overridden Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            BEncodedNumber obj2 = obj as BEncodedNumber;
            return obj2 != null && this.Number == obj2.Number;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.Number.GetHashCode();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return (this.Number.ToString());
        }
        #endregion
    }
}
