﻿//
// Copyright (c) 2008, Kenneth Bell
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace DiscUtils.Iso9660
{
    internal class Utilities
    {
        public static uint ToUInt32FromBoth(byte[] data, int offset)
        {
            return BitConverter.ToUInt32(data, offset);
        }

        public static ushort ToUInt16FromBoth(byte[] data, int offset)
        {
            return BitConverter.ToUInt16(data, offset);
        }

        internal static void ToBothFromUInt32(byte[] buffer, int offset, uint value)
        {
            byte[] bytes;

            bytes = BitConverter.GetBytes(value);
            Array.Copy(bytes, 0, buffer, offset, 4);
            bytes = BitConverter.GetBytes(ByteSwap(value));
            Array.Copy(bytes, 0, buffer, offset + 4, 4);
        }

        internal static void ToBothFromUInt16(byte[] buffer, int offset, ushort value)
        {
            byte[] bytes;

            bytes = BitConverter.GetBytes(value);
            Array.Copy(bytes, 0, buffer, offset, 2);
            bytes = BitConverter.GetBytes(ByteSwap(value));
            Array.Copy(bytes, 0, buffer, offset + 2, 2);
        }

        internal static void ToBytesFromUInt32(byte[] buffer, int offset, uint value)
        {
            byte[] bytes;
            bytes = BitConverter.GetBytes(value);
            Array.Copy(bytes, 0, buffer, offset, 4);
        }

        internal static void ToBytesFromUInt16(byte[] buffer, int offset, ushort value)
        {
            byte[] bytes;
            bytes = BitConverter.GetBytes(value);
            Array.Copy(bytes, 0, buffer, offset, 2);
        }

        public static uint ByteSwap(uint val)
        {
            return ((val >> 24) & 0x000000FF) | ((val >> 8) & 0x0000FF00) | ((val << 8) & 0x00FF0000) | ((val << 24) & 0xFF000000);
        }

        public static ushort ByteSwap(ushort val)
        {
            return (ushort)(((val >> 8) & 0x00FF) | ((val << 8) & 0xFF00));
        }

        public static void WriteAChars(byte[] buffer, int offset, int numBytes, String str)
        {
            // Validate string
            if (!isValidAString(str))
            {
                throw new IOException("Attempt to write string with invalid a-characters");
            }

            //WriteASCII(buffer, offset, numBytes, true, str);
            WriteString(buffer, offset, numBytes, true, str, Encoding.ASCII);
        }

        public static void WriteDChars(byte[] buffer, int offset, int numBytes, String str)
        {
            // Validate string
            if (!isValidDString(str))
            {
                throw new IOException("Attempt to write string with invalid d-characters");
            }

            //WriteASCII(buffer, offset, numBytes, true, str);
            WriteString(buffer, offset, numBytes, true, str, Encoding.ASCII);
        }

        public static void WriteA1Chars(byte[] buffer, int offset, int numBytes, String str, Encoding enc)
        {
            // Validate string
            if (!isValidAString(str))
            {
                throw new IOException("Attempt to write string with invalid a-characters");
            }

            WriteString(buffer, offset, numBytes, true, str, enc);
        }

        public static void WriteD1Chars(byte[] buffer, int offset, int numBytes, String str, Encoding enc)
        {
            // Validate string
            if (!isValidDString(str))
            {
                throw new IOException("Attempt to write string with invalid d-characters");
            }

            WriteString(buffer, offset, numBytes, true, str, enc);
        }

        public static string ReadChars(byte[] buffer, int offset, int numBytes, Encoding enc)
        {
            char[] chars;

            // Special handling for 'magic' names '\x00' and '\x01', which indicate root and parent, respectively
            if (numBytes == 1)
            {
                chars = new char[1];
                chars[0] = (char)buffer[offset];
            }
            else
            {
                Decoder decoder = enc.GetDecoder();
                chars = new char[decoder.GetCharCount(buffer, offset, numBytes, false)];
                decoder.GetChars(buffer, offset, numBytes, chars, 0, false);
            }

            return new string(chars).TrimEnd(' ');
        }

        public static byte WriteFileName(byte[] buffer, int offset, int numBytes, String str, Encoding enc)
        {
            if (numBytes > 255 || numBytes < 0)
            {
                throw new ArgumentOutOfRangeException("numBytes", "Attempt to write overlength or underlength file name");
            }

            // Validate string
            if (!isValidFileName(str))
            {
                throw new IOException("Attempt to write string with invalid file name characters");
            }

            return (byte)WriteString(buffer, offset, numBytes, false, str, enc);
        }

        public static byte WriteDirectoryName(byte[] buffer, int offset, int numBytes, String str, Encoding enc)
        {
            if (numBytes > 255 || numBytes < 0)
            {
                throw new ArgumentOutOfRangeException("numBytes", "Attempt to write overlength or underlength directory name");
            }

            // Validate string
            if (!isValidDirectoryName(str))
            {
                throw new IOException("Attempt to write string with invalid directory name characters");
            }

            return (byte)WriteString(buffer, offset, numBytes, false, str, enc);
        }

        public static int WriteString(byte[] buffer, int offset, int numBytes, bool pad, String str, Encoding enc)
        {
            Encoder encoder = enc.GetEncoder();

            string paddedString = pad ? str + new string(' ', numBytes) : str; // Assumption: never less than one byte per character

            int charsUsed;
            int bytesUsed;
            bool completed;
            encoder.Convert(paddedString.ToCharArray(), 0, paddedString.Length, buffer, offset, numBytes, false, out charsUsed, out bytesUsed, out completed);

            if (charsUsed < str.Length)
            {
                throw new IOException("Failed to write entire string");
            }

            return bytesUsed;
        }

        public static bool isValidAString(String str)
        {
            for (int i = 0; i < str.Length; ++i)
            {
                if (!(
                    (str[i] >= ' ' && str[i] <= '\"')
                    || (str[i] >= '%' && str[i] <= '/')
                    || (str[i] >= ':' && str[i] <= '?')
                    || (str[i] >= '0' && str[i] <= '9')
                    || (str[i] >= 'A' && str[i] <= 'Z')
                    || (str[i] == '_')))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool isValidDString(String str)
        {
            for (int i = 0; i < str.Length; ++i)
            {
                if (!isValidDChar(str[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool isValidDChar(char ch)
        {
            return (ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || (ch == '_');
        }

        public static bool isValidFileName(String str)
        {
            for (int i = 0; i < str.Length; ++i)
            {
                if (!((str[i] >= '0' && str[i] <= '9') || (str[i] >= 'A' && str[i] <= 'Z') || (str[i] == '_') || (str[i] == '.') || (str[i] == ';')))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool isValidDirectoryName(String str)
        {
            if (str.Length == 1 && (str[0] == 0 || str[0] == 1))
            {
                return true;
            }
            else
            {
                return isValidDString(str);
            }
        }

        public static string NormalizeFileName(string name)
        {
            string[] parts = SplitFileName(name);
            return parts[0] + '.' + parts[1] + ';' + parts[2];
        }

        public static string[] SplitFileName(string name)
        {
            string[] parts = new string[] { name, "", "1" };

            if (name.Contains('.'))
            {
                int endOfFilePart = name.IndexOf('.');
                parts[0] = name.Substring(0, endOfFilePart);
                if (name.Contains(';'))
                {
                    int verSep = name.IndexOf(';', endOfFilePart + 1);
                    parts[1] = name.Substring(endOfFilePart + 1, verSep - (endOfFilePart + 1));
                    parts[2] = name.Substring(verSep + 1);
                }
                else
                {
                    parts[1] = name.Substring(endOfFilePart + 1);
                }
            }
            else
            {
                if (name.Contains(';'))
                {
                    int verSep = name.IndexOf(';');
                    parts[0] = name.Substring(0, verSep);
                    parts[2] = name.Substring(verSep + 1);
                }
            }

            ushort ver;
            if (!UInt16.TryParse(parts[2], out ver) || ver > 32767 || ver < 1)
            {
                ver = 1;
            }
            parts[2] = String.Format("{0}", ver);

            return parts;
        }

        /// <summary>
        /// Converts a DirectoryRecord time to UTC.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static DateTime ToUTCDateTimeFromDirectoryTime(byte[] data, int offset)
        {
            DateTime relTime = new DateTime(
                1900 + data[offset],
                data[offset + 1],
                data[offset + 2],
                data[offset + 3],
                data[offset + 4],
                data[offset + 5]);
            return relTime + TimeSpan.FromMinutes(15 * (sbyte)data[offset + 6]);
        }

        internal static void ToDirectoryTimeFromUTC(byte[] data, int offset, DateTime dateTime)
        {
            if (dateTime.Year < 1900)
            {
                throw new IOException("Year is out of range");
            }

            data[offset] = (byte)(dateTime.Year - 1900);
            data[offset + 1] = (byte)dateTime.Month;
            data[offset + 2] = (byte)dateTime.Day;
            data[offset + 3] = (byte)dateTime.Hour;
            data[offset + 4] = (byte)dateTime.Minute;
            data[offset + 5] = (byte)dateTime.Second;
            data[offset + 6] = 0;
        }

        public static DateTime ToDateTimeFromVolumeDescriptorTime(byte[] data, int offset)
        {
            bool nonNull = false;
            for (int i = 0; i < 16; ++i)
            {
                if (data[offset + i] != (byte)'0' && data[offset + i] != 0)
                {
                    nonNull = true;
                    break;
                }
            }

            if (!nonNull)
            {
                return DateTime.MinValue;
            }

            // Note: work around bugs in burning software that put zero bytes (rather than '0' characters for fractions)
            if (data[offset + 14] == 0) { data[offset + 14] = (byte)'0'; }
            if (data[offset + 15] == 0) { data[offset + 15] = (byte)'0'; }

            string strForm = Encoding.ASCII.GetString(data, offset, 16);
            return DateTime.ParseExact(strForm, "yyyyMMddHHmmssff", CultureInfo.InvariantCulture) + TimeSpan.FromMinutes(15 * (sbyte)data[offset + 16]);
        }

        internal static void ToVolumeDescriptorTimeFromUTC(byte[] buffer, int offset, DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue)
            {
                for (int i = offset; i < offset + 16; ++i)
                {
                    buffer[i] = (byte)'0';
                }
                buffer[offset + 16] = 0;
                return;
            }

            string strForm = dateTime.ToString("yyyyMMddHHmmssff", CultureInfo.InvariantCulture);
            Array.Copy(Encoding.ASCII.GetBytes(strForm), 0, buffer, offset, 16);
            buffer[offset + 16] = 0;
        }

        internal static void EncodingToBytes(Encoding enc, byte[] data, int offset)
        {
            Array.Clear(data, offset, 32);
            if (enc == Encoding.ASCII)
            {
                // Nothing to do
            }
            else if (enc == Encoding.BigEndianUnicode)
            {
                data[offset + 0] = 0x25;
                data[offset + 1] = 0x2F;
                data[offset + 2] = 0x45;
            }
            else
            {
                throw new ArgumentOutOfRangeException("Unrecognized character encoding");
            }
        }

        internal static Encoding EncodingFromBytes(byte[] data, int offset)
        {
            Encoding enc = Encoding.ASCII;
            if (data[offset + 0] == 0x25 && data[offset + 1] == 0x2F
                && (data[offset + 2] == 0x40 || data[offset + 2] == 0x43 || data[offset+2] == 0x45))
            {
                // I.e. this is a joliet disc!
                enc = Encoding.BigEndianUnicode;
            }

            return enc;
        }

        internal static int ReadFully(Stream stream, byte[] buffer, int offset, int length)
        {
            int totalRead = 0;
            int numRead = stream.Read(buffer, offset, length);
            while (numRead > 0)
            {
                totalRead += numRead;
                numRead = stream.Read(buffer, offset + totalRead, length - totalRead);
            }

            return totalRead;
        }

        internal static bool IsSpecialDirectory(DirectoryRecord r)
        {
            if ((r.Flags & FileFlags.Directory) != 0)
            {
                return r.FileIdentifier == "\0" || r.FileIdentifier == "\x01";
            }
            else
            {
                return false;
            }
        }
    }
}