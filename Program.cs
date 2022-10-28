using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SystemPlus;
using SystemPlus.Extensions;

namespace TextVideoViewer
{
    public static class Program
    {
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [STAThread]
        static void Main(string[] args)
        {
            start:
            OpenFileDialog dialog = new OpenFileDialog()
            {
                AddExtension = true,
                DefaultExt = ".tvf",
                CheckFileExists = true,
                Filter = "TextVideo|*.tvf",
                Multiselect = false,
                ShowHelp = false,
                Title = "Select TextVideoFile",
            };
            DialogResult res = dialog.ShowDialog();

            if (res != DialogResult.OK) {
                NativeWindow nw = NativeWindow.FromHandle(Process.GetCurrentProcess().MainWindowHandle);
                MessageBox.Show(nw, "You need to click \"Open\" to comfirm", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                goto start;
            } else if (!File.Exists(dialog.FileName)) {
                NativeWindow nw = NativeWindow.FromHandle(Process.GetCurrentProcess().MainWindowHandle);
                MessageBox.Show(nw, "Selected file doesn't exist", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                goto start;
            }
            else if (Path.GetExtension(dialog.FileName) != ".tvf") {
                NativeWindow nw = NativeWindow.FromHandle(Process.GetCurrentProcess().MainWindowHandle);
                MessageBox.Show(nw, $"TextVideoFile wasn't selected (selected:{Path.GetExtension(dialog.FileName)})", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                goto start;
            }

            SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);

            SetUpAndPlay(dialog.FileName);
        }

        static void SetUpAndPlay(string path)
        {
            byte b = CheckChecksum(path);

            if (b == 1) {
                Console.WriteLine("File is corrupted or incorrect format");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
                return;
            } else if (b == 2) {
                Console.WriteLine("Checksum is incorrect");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
                return;
            }

            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            stream.Read(new byte[8], 0, 8); // skip checksum
            uint frameCount = BitConverter.ToUInt32(stream.Read(4), 0);
            double fps = BitConverter.ToDouble(stream.Read(8), 0);
            ushort width = BitConverter.ToUInt16(stream.Read(2), 0);
            ushort height = BitConverter.ToUInt16(stream.Read(2), 0);
            byte c = stream.Read(1)[0];
            VideoConfig config = new VideoConfig();
            config.Value = c;

            Console.WriteLine($"Loaded video:\nFPS: {MathPlus.Round(fps, 2)}\nWidth: {width}, Height: {height}\nframe count: {frameCount}\nconfig: {config}");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
            Console.Clear();

            SetUpView(width, height, config);
            Play(stream, width, height, config, fps, frameCount);

            stream.Close();
            stream.Dispose();

            Console.ResetColor();
            
            Console.Clear();
            ConsoleExtensions.SetFontSize(16);
            Console.CursorVisible = true;
            Console.SetWindowSize(50, 6);
            Console.WriteLine("Done playing video");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
        }

        public static byte CheckChecksum(string path)
        {
            byte[] buf = File.ReadAllBytes(path);

            if (buf.Length < 25)
                return 1;

            ulong expectedChecksum = BitConverter.ToUInt64(buf, 0);
            ulong checksum = 0;

            for (int i = 8; i < buf.Length; i++)
                checksum += buf[i];

            if (expectedChecksum != checksum)
                return 2;
            else
                return 0;
        }

        public static void SetUpView(int width, int height, VideoConfig config)
        {
            short fs = 12;
            if (config.FontSize == 0)
                fs = 2;
            else if (config.FontSize == 1)
                fs = 6;
            else if (config.FontSize == 2)
                fs = 12;

            ConsoleExtensions.SetFontSize(fs);
            SetSize(width, height, config.SquarePixels);
            Console.CursorVisible = false;
        }

        public static void SetSize(int width, int height, bool squarePixels)
        {
            if (squarePixels) {
                Console.SetBufferSize(width * 2 + 1, height);
                Console.SetWindowSize(width * 2 + 1, height);
            } else {
                Console.SetBufferSize(width + 1, height);
                Console.SetWindowSize(width + 1, height);
            }
        }

        public static void Play(Stream stream, ushort width, ushort height, VideoConfig config, double fps, uint frameCount)
        {
            byte[] buffer = new byte[(width + 1) * height];
            if (config.SquarePixels)
                buffer = new byte[(width * 2 + 1) * height];

            if (config.VideoType == 1) {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.White;
            }

            Stopwatch watch = new Stopwatch();

            long milis = (long)((1d / fps) * 1000d);

            for (uint i = 0; i < frameCount; i++) {
                watch.Start();
                int read = stream.Read(buffer, 0, buffer.Length);
                if (read < buffer.Length) {
                    return;
                }

                Console.SetCursorPosition(0, 0);
                string s = Encoding.ASCII.GetString(buffer);
                Console.Write(s);

                while (watch.ElapsedMilliseconds < milis) { }

                Console.Title = $"Playback FPS: {MathPlus.Round(1d / ((double)watch.ElapsedMilliseconds / 1000d), 3)}, {i}/{frameCount - 1}";

                watch.Reset();
            }
        }

        static byte[] Read(this Stream stream, int count)
        {
            byte[] a = new byte[count];
            stream.Read(a, 0, count);
            return a;
        }
    }
}
