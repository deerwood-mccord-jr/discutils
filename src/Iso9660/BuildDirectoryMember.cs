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
using System.Collections.Generic;
using System.Text;

namespace DiscUtils.Iso9660
{
    public abstract class BuildDirectoryMember
    {
        private string name;
        private string shortName;
        private DateTime creationTime;

        public BuildDirectoryMember(string name, string shortName)
        {
            this.name = name;
            this.shortName = shortName;
            creationTime = DateTime.UtcNow;
        }

        public string Name
        {
            get { return name; }
        }

        public string ShortName
        {
            get { return shortName; }
        }

        public abstract BuildDirectoryInfo Parent
        {
            get;
        }

        public DateTime CreationTime
        {
            get { return creationTime; }
            set { creationTime = value; }
        }

        internal string PickName(string nameOverride, Encoding enc)
        {
            if (nameOverride != null)
            {
                return nameOverride;
            }
            else
            {
                return (enc == Encoding.ASCII) ? ShortName : Name;
            }
        }


        internal abstract long GetDataSize(Encoding enc);

        internal uint GetDirectoryRecordSize(Encoding enc)
        {
            return DirectoryRecord.CalcLength(PickName(null, enc), enc);
        }

        private class DirectorySortedComparison : Comparer<BuildDirectoryMember>
        {
            public override int Compare(BuildDirectoryMember x, BuildDirectoryMember y)
            {
                string[] xParts = x.Name.Split('.', ';');
                string[] yParts = y.Name.Split('.', ';');

                string xPart;
                string yPart;

                for (int i = 0; i < 2; ++i)
                {
                    xPart = (xParts.Length > i) ? xParts[i] : "";
                    yPart = (yParts.Length > i) ? yParts[i] : "";
                    int val = ComparePart(xPart, yPart, ' ');
                    if (val != 0)
                    {
                        return val;
                    }
                }

                xPart = (xParts.Length > 2) ? xParts[2] : "";
                yPart = (yParts.Length > 2) ? yParts[2] : "";
                return ComparePartBackwards(xPart, yPart, '0');
            }

            private static int ComparePart(string x, string y, char padChar)
            {
                int max = Math.Max(x.Length, y.Length);
                for (int i = 0; i < max; ++i)
                {
                    char xChar = (i < x.Length) ? x[i] : padChar;
                    char yChar = (i < y.Length) ? y[i] : padChar;

                    if (xChar != yChar)
                    {
                        return xChar - yChar;
                    }
                }

                return 0;
            }

            private static int ComparePartBackwards(string x, string y, char padChar)
            {
                int max = Math.Max(x.Length, y.Length);

                int xPad = max - x.Length;
                int yPad = max - y.Length;

                for (int i = 0; i < max; ++i)
                {
                    char xChar = (i >= xPad) ? x[i - xPad] : padChar;
                    char yChar = (i >= yPad) ? y[i - yPad] : padChar;

                    if (xChar != yChar)
                    {
                        // Note: Version numbers are in DESCENDING order!
                        return yChar - xChar;
                    }
                }

                return 0;
            }
        }

        internal static readonly Comparer<BuildDirectoryMember> SortedComparison = new DirectorySortedComparison();
    }

}