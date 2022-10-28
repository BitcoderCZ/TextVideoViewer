using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace TextVideoViewer
{
    public class Screen
    {
        #region PInvoke
        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern SafeFileHandle CreateFile(string fileName, [MarshalAs(UnmanagedType.U4)] uint fileAccess, [MarshalAs(UnmanagedType.U4)] uint fileShare,
        IntPtr securityAttributes, [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition, [MarshalAs(UnmanagedType.U4)] int flags, IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteConsoleOutput(SafeFileHandle hConsoleOutput, CharInfo[] lpBuffer, Coord dwBufferSize, Coord dwBufferCoord, ref SmallRect lpWriteRegion);

        [StructLayout(LayoutKind.Sequential)]
        public struct Coord
        {
            public short X;
            public short Y;

            public Coord(short _X, short _Y)
            {
                X = _X;
                Y = _Y;
            }
        };

        [StructLayout(LayoutKind.Explicit)]
        public struct CharUnion
        {
            [FieldOffset(0)] public char UnicodeChar;
            [FieldOffset(0)] public byte AsciiChar;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct CharInfo
        {
            [FieldOffset(0)] public CharUnion Char;
            [FieldOffset(2)] public short Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SmallRect
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;

            public int X { get => Left; set { Right = (short)(value + Width); Left = (short)value; } }
            public int Y { get => Top; set { Bottom = (short)(value + Height); Top = (short)value; } }
            public int Width { get => Right - Left; set => Right = (short)(Left + value); }
            public int Height { get => Bottom - Top; set => Bottom = (short)(Top + value); }

            public SmallRect(short _left, short _top, short _right, short _bottom)
            {
                Left = _left;
                Top = _top;
                Right = _right;
                Bottom = _bottom;
            }

            public static SmallRect Create(short X, short Y, short Width, short Height) => new SmallRect(X, Y, (short)(X + Width), (short)(Y + Height));
        }

        [DllImport("kernel32.dll")]
        static extern bool WriteConsoleOutputCharacter(IntPtr hConsoleOutput, char[] lpCharacter, uint nLength, Coord dwWriteCoord, out uint lpNumberOfCharsWritten);

        const int STD_OUTPUT_HANDLE = -11;
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);
        #endregion

        private static SafeFileHandle h;
        private static IntPtr h2;

        public static void Init()
        {
            h = CreateFile("CONOUT$", 0x40000000, 2, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);

            if (h.IsInvalid)
                throw new Exception("Couln't create SafeFileHandle");

            h2 = GetStdHandle(STD_OUTPUT_HANDLE);
        }

        public static bool DrawChars(char[] array, Coord coord)
        {
            return WriteConsoleOutputCharacter(h2, array, (uint)array.Length, coord, out uint _);
        }

        public static bool Draw(Screen s)
        {
            if (h.IsInvalid)
                throw new Exception("Handle isn't valid");

            return WriteConsoleOutput(h, s.buffer, new Coord((short)s.Width, (short)s.Height), new Coord((short)s.X, (short)s.Y), ref s.rect);
        }

        public CharInfo[] buffer;
        public SmallRect rect;

        public int X { get => rect.Left; set => rect.X = value; }
        public int Y { get => rect.Top; set => rect.Y = value; }
        public int Width { get => rect.Width; set => rect.Width = value; }
        public int Height { get => rect.Height; set => rect.Height = value; }

        public Screen(int X, int Y, int Width, int Height)
        {
            if (Width < 0 || Height < 0)
                throw new Exception("Width or Height less than zero");

            buffer = new CharInfo[Width * Height];
            rect = SmallRect.Create((short)X, (short)Y, (short)Width, (short)Height);
        }

        public Screen() : this(0, 0, 1, 1)
        { }

        public void Clear(CharInfo info)
        {
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = info;
        }

        public void Clear(ConsoleColor c)
        {
            CharInfo info = new CharInfo() { Attributes = (short)((short)c << 4), Char = new CharUnion() { UnicodeChar = ' ' } };
            Clear(info);
        }

        public void DrawString(string s, ConsoleColor c, int x, int y)
        {
            int baseCoor = y * Width + x;
            for (int i = 0; i < s.Length; i++)
                buffer[baseCoor + i] = new CharInfo() { Char = new CharUnion() { UnicodeChar = s[i] }, Attributes = (short)((int)c | (buffer[baseCoor + i].Attributes & 0b_0000_0000_1111_0000)) };
        }

        public void DrawString(string s, ConsoleColor c, ConsoleColor bc, int x, int y)
        {
            int baseCoor = y * Width + x;
            for (int i = 0; i < s.Length; i++)
                buffer[baseCoor + i] = new CharInfo() { Char = new CharUnion() { UnicodeChar = s[i] }, Attributes = (short)((short)c | (short)((short)bc << 4)) };
        }

        public void DrawRectC(ConsoleColor c, int x, int y, int width, int height)
        {
            CharInfo info = new CharInfo() { Attributes = (short)((short)c << 4), Char = new CharUnion() { UnicodeChar = ' ' } };

            int right = x + width;
            int bottom = y + height;

            for (int _x = x; _x < right; _x++)
                for (int _y = y; _y < bottom; _y++)
                    if (_x == x || _x == right - 1 || _y == y || _y == bottom - 1)
                        buffer[_y * Width + _x] = info;
        }

        public void FillRectC(ConsoleColor c, int x, int y, int width, int height)
        {
            CharInfo info = new CharInfo() { Attributes = (short)((short)c << 4), Char = new CharUnion() { UnicodeChar = ' ' } };
            for (int _x = x; _x < x + width; _x++)
                for (int _y = y; _y < y + height; _y++)
                    buffer[_y * Width + _x] = info;
        }

        // ***************************Special***************************
        public void DrawBtn(string s, ConsoleColor bc, ConsoleColor tc, bool down, int x, int y, int width, int height)
        {
            if (down) {
                FillRectC(bc, x + 1, y + 1, width, height);
                DrawString(s, tc, x + 2, y + 2);
            }
            else {
                FillRectC(ConsoleColor.DarkGray, x + 1, y + 1, width, height);
                FillRectC(bc, x, y, width, height);
                DrawString(s, tc, x + 1, y + 1);
            }
        }

        public void DrawBtn(string s, ConsoleColor bc, ConsoleColor tc, bool down, int x, int y) => DrawBtn(s, bc, tc, down, x, y, s.Length + 2, 3);
    }
}