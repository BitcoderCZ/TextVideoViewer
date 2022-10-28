using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TextVideoViewer
{
    [StructLayout(LayoutKind.Explicit)]
    public struct VideoConfig
    {
        [FieldOffset(0)]
        private byte squarePixels;
        [FieldOffset(1)]
        private byte encoding;
        [FieldOffset(2)]
        private byte videoType;
        [FieldOffset(3)]
        private byte fontSize; // 0 - 2, 1 - 6, 2 - 12, 3 - ?

        public bool SquarePixels { get => (squarePixels & 1) == 1 ? true : false; 
            set {
                if (SquarePixels != value)
                    squarePixels ^= 1;
            }
        }

        public byte Encoding { get => (byte)(encoding & 0b_111); 
            set {
                byte v = (byte)(value & 0b_111);
                encoding = v;
            }
        }

        public byte VideoType { get => (byte)(videoType & 0b_11); 
             set {
                byte v = (byte)(value & 0b_11);
                videoType = v;
             }
        }

        public byte FontSize
        {
            get => (byte)(fontSize & 0b_11);
            set {
                byte v = (byte)(value & 0b_11);
                fontSize = v;
            }
        }

        public VideoConfig(bool _squarePixels, byte _encoding, byte _type, byte _fontSize)
        {
            squarePixels = 0;
            encoding = 0;
            videoType = 0;
            fontSize = 0;

            SquarePixels = _squarePixels;
            Encoding = _encoding;
            VideoType = _type;
            FontSize = _fontSize;
        }

        public byte Value {
            get {
                byte v = 0;
                v |= squarePixels;
                v |= (byte)(encoding << 1);
                v |= (byte)(videoType << 4);
                v |= (byte)(fontSize << 6);
                return v;
            }
            set {
                squarePixels = (byte)(value & 0b_0000_0001);
                encoding =     (byte)((value & 0b_0000_1110) >> 1);
                videoType =    (byte)((value & 0b_0011_0000) >> 4);
                fontSize =    (byte)((value & 0b_1100_0000) >> 6);
            }
        }

        public override string ToString() => $"Square pixels: {SquarePixels}, Encoding: {Encoding}, Video type: {VideoType}, Font size: {FontSize}";
    }
}
