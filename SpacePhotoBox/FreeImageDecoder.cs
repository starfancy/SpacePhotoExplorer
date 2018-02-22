using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;

using FreeImageAPI;

namespace StudioFancy.SpacePhotoBox
{
    using FIBITMAP = UInt32;
    using FIMEMORY = UInt32;
   
    using HDC = IntPtr;
    using HWnd = IntPtr;
    using HBitmap = IntPtr;
    using HPalette = IntPtr;
    using BITMAPINFO = IntPtr;
    
    class FreeImageDecoder: Decoder
    {
        
        public FreeImageDecoder()
        {
        }
      
        unsafe struct FIMemory
        {
            public void* pData;
        }

        public override Photo Decode(string file)
        {
            byte[] buffer = File.ReadAllBytes(file);
            int nBufferLength = buffer.Length;
            uint nImageWidth, nImageHeight;
            uint nBitsPerPixel;
            uint nBitsCount;
            //byte[] imageBits;
            NativePhoto photo = null;
            unsafe
            {
                fixed (byte* pBuffer = buffer)
                {
                    FIMEMORY fiMemory = FreeImage.OpenMemory((IntPtr)pBuffer, nBufferLength);
                    FREE_IMAGE_FORMAT fiFormat = FreeImage.GetFileTypeFromMemory(fiMemory, nBufferLength);
                    FIBITMAP fiBitmap = FreeImage.LoadFromMemory(fiFormat, fiMemory, 0);
                    FREE_IMAGE_COLOR_TYPE colorType = FreeImage.GetColorType(fiBitmap);

                    HBitmap hBitmap;
                    HPalette hPalette = IntPtr.Zero, hOldPalette = IntPtr.Zero;
                    nImageHeight = FreeImage.GetHeight(fiBitmap);
                    nImageWidth = FreeImage.GetWidth(fiBitmap);
                    nBitsPerPixel = FreeImage.GetBPP(fiBitmap);

                    //if (nBitsPerPixel <= 8)
                    //{
                    //    RGBQUAD[] rgb = FreeImage.GetPaletteCopy(fiBitmap);
                    //    LOGPALETTE logPalette = new LOGPALETTE();
                    //    logPalette.nPalVersion = 0x300;
                    //    logPalette.nPalEntries = 1 << 8;
                    //    PaletteEntry[] palEntries = new PaletteEntry[logPalette.nPalEntries];
                    //    for (int i = 0; i < logPalette.nPalEntries; i++)
                    //    {
                    //        palEntries[i].Blue = rgb[i].rgbBlue;
                    //        palEntries[i].Green = rgb[i].rgbGreen;
                    //        palEntries[i].Red = rgb[i].rgbRed;
                    //    }
                    //    fixed (PaletteEntry* pPalletteEntries = palEntries)
                    //    {
                    //        logPalette.pPalEntry = (IntPtr)pPalletteEntries;
                    //        hPalette = CreatePalette(&logPalette);
                    //    }
                    //}

                    HDC hDC = GetDC(IntPtr.Zero);
                    Debug.Assert(hDC!= IntPtr.Zero);
                    //if (hPalette != IntPtr.Zero)
                    //{
                    //    hOldPalette = SelectPalette(hDC, hPalette, false);
                    //    RealizePalette(hDC);
                    //}
                    hBitmap = CreateCompatibleBitmap(hDC, (int)nImageWidth, (int)nImageHeight);
                    //    hBitmap = CreateDIBitmap(hDC, FreeImage.FreeImage_GetInfoHeader(fiBitmap), 0x04,
                    //FreeImage.GetBits(fiBitmap), FreeImage.FreeImage_GetInfo(fiBitmap), ColorUseOptions.DIB_PAL_COLORS);
                    int nResult = SetDIBits(hDC, hBitmap, 0, nImageHeight, FreeImage.GetBits(fiBitmap),
                        FreeImage.FreeImage_GetInfo(fiBitmap), ColorUseOptions.DIB_RGB_COLORS);
                    Debug.Assert(nResult != 0);

                    if (hOldPalette != IntPtr.Zero)
                        SelectPalette(hDC, hOldPalette, false);

                    int width = (int)(FreeImage.GetWidth(fiBitmap));
                    int height = (int)(FreeImage.GetHeight(fiBitmap));
                    photo = new NativePhoto(hBitmap, width, height);

                    ReleaseDC(IntPtr.Zero, hDC);
                    //if (hPalette != IntPtr.Zero)
                    //    info.PaletteHandle = hPalette;

                    FreeImage.Unload(fiBitmap);
                    FreeImage.CloseMemory(fiMemory);
                }
            }
            return photo;
        }

        protected FREE_IMAGE_FORMAT ImageFormatTofiImageFormat(ImageFormat format)
        {
            FREE_IMAGE_FORMAT fiFormat = FREE_IMAGE_FORMAT.FIF_UNKNOWN;
            if(format == ImageFormat.Jpeg)
                    fiFormat = FREE_IMAGE_FORMAT.FIF_JPEG;
                return fiFormat;
        }

        /// <summary>
        /// GetDC in Windows GDI
        /// ReleaseDC must be called in the same method which invoke GetDC
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
       public static extern HDC GetDC(HWnd hWnd);
        
        /// <summary>
        /// ReleaseDC in Windows GDI
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="hDC"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern int ReleaseDC
         (
          HWnd hWnd,  // handle to window
          HDC hDC     // handle to DC
        );

        /// <summary>
        /// DeleteObject
        /// </summary>
        [DllImport("gdi32.dll", ExactSpelling = true,
            SetLastError = true)]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("Gdi32.dll")]
        unsafe public static extern HBitmap CreateBitmap
        (
        int nWidth,         // bitmap width, in pixels
        int nHeight,        // bitmap height, in pixels
        uint cPlanes,       // number of color planes
        uint cBitsPerPel,   // number of bits to identify color
        void* lpvBits // color data array
        );

        [DllImport("Gdi32.dll")]
        public static extern HBitmap CreateBitmap
        (
        int nWidth,         // bitmap width, in pixels
        int nHeight,        // bitmap height, in pixels
        uint cPlanes,       // number of color planes
        uint cBitsPerPel,   // number of bits to identify color
        IntPtr lpvBits // color data array
        );

        [DllImport("Gdi32.dll")]
        public static extern HBitmap CreateDIBitmap
         (
          HDC hdc,                        // handle to DC
          IntPtr lpbmih, // bitmap data
          int fdwInit,                  // initialization option
          IntPtr lpbInit,            // initialization data
          IntPtr lpbmi,        // color-format data
          ColorUseOptions fuUsage                    // color-data usage
        );

        /// <summary>
        /// CreateCompatibleBitmap
        /// </summary>
        [DllImport("gdi32.dll", ExactSpelling = true,
            SetLastError = true)]
        public static extern IntPtr CreateCompatibleBitmap(
            IntPtr hObject, int width, int height);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern int SetDIBits
         (
        HDC hdc,                  // handle to DC
        HBitmap hbmp,             // handle to bitmap
        uint uStartScan,          // starting scan line
        uint cScanLines,          // number of scan lines
        IntPtr lpvBits,      // array of bitmap bits
        BITMAPINFO lpbmi,  // bitmap data
        ColorUseOptions fuColorUse           // type of color indexes to use
        );

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern HPalette SelectPalette
        (
          HDC hdc,                // handle to DC
          HPalette hpal,          // handle to logical palette
          bool bForceBackground   // foreground or background mode
        );

        public enum ColorUseOptions
        {
            DIB_RGB_COLORS = 0,
            DIB_PAL_COLORS = 1
        };

        
        public struct LOGPALETTE
        {
            public ushort nPalVersion;
            public ushort nPalEntries;
            public IntPtr pPalEntry;
        };

        public struct PaletteEntry
        {
            public byte Red;
            public byte Green;
            public byte Blue;
            public byte Flag;
        }

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern uint RealizePalette(
         HDC hdc   // handle to DC
        );

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        unsafe public static extern HPalette CreatePalette(
             LOGPALETTE* lplgpl   // logical palette
        );

        /// <summary>
        /// Enumeration for the raster operations used in BitBlt.
        /// In C++ these are actually #define. But to use these
        /// constants with C#, a new enumeration type is defined.
        /// </summary>
        public enum TernaryRasterOperations
        {
            SRCCOPY = 0x00CC0020, // dest = source
            SRCPAINT = 0x00EE0086, // dest = source OR dest
            SRCAND = 0x008800C6, // dest = source AND dest
            SRCINVERT = 0x00660046, // dest = source XOR dest
            SRCERASE = 0x00440328, // dest = source AND (NOT dest)
            NOTSRCCOPY = 0x00330008, // dest = (NOT source)
            NOTSRCERASE = 0x001100A6, // dest = (NOT src) AND (NOT dest)
            MERGECOPY = 0x00C000CA, // dest = (source AND pattern)
            MERGEPAINT = 0x00BB0226, // dest = (NOT source) OR dest
            PATCOPY = 0x00F00021, // dest = pattern
            PATPAINT = 0x00FB0A09, // dest = DPSnoo
            PATINVERT = 0x005A0049, // dest = pattern XOR dest
            DSTINVERT = 0x00550009, // dest = (NOT dest)
            BLACKNESS = 0x00000042, // dest = BLACK
            WHITENESS = 0x00FF0062, // dest = WHITE
        };
    }
}
