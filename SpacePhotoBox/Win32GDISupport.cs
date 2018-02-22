using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;

namespace StudioFancy
{
    namespace SpacePhotoBox
    {
        using HBitmap = IntPtr;
        using HPalette = IntPtr;
        using HDC = IntPtr;
        
        public class Win32GDISupport
        {
            /// <summary>
            /// Enumeration to be used for those Win32 function 
            /// that return BOOL
            /// </summary>
            public enum Bool
            {
                False = 0,
                True
            };

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

            /// <summary>
            /// CreateCompatibleDC
            /// </summary>
            [DllImport("gdi32.dll", ExactSpelling = true,
                SetLastError = true)]
            public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

            /// <summary>
            /// DeleteDC
            /// </summary>
            [DllImport("gdi32.dll", ExactSpelling = true,
                SetLastError = true)]
            public static extern Bool DeleteDC(IntPtr hdc);

            /// <summary>
            /// SelectObject
            /// </summary>
            [DllImport("gdi32.dll", ExactSpelling = true)]
            public static extern IntPtr SelectObject(IntPtr hDC,
                IntPtr hObject);

            /// <summary>
            /// DeleteObject
            /// </summary>
            [DllImport("gdi32.dll", ExactSpelling = true,
                SetLastError = true)]
            public static extern Bool DeleteObject(IntPtr hObject);

            /// <summary>
            /// CreateCompatibleBitmap
            /// </summary>
            [DllImport("gdi32.dll", ExactSpelling = true,
                SetLastError = true)]
            public static extern IntPtr CreateCompatibleBitmap(
                IntPtr hObject, int width, int height);

            /// <summary>
            /// BitBlt
            /// </summary>
            [DllImport("gdi32.dll", ExactSpelling = true,
                SetLastError = true)]
            public static extern Bool BitBlt(
                IntPtr hObject,
                int nXDest, int nYDest,
                int nWidth, int nHeight,
                IntPtr hObjSource, int nXSrc, int nYSrc,
                TernaryRasterOperations dwRop);

            /// <summary>
            /// ExcludeClipRect
            /// </summary>
            [DllImport("gdi32.dll", ExactSpelling = true,
                 SetLastError = true)]
            public static extern int ExcludeClipRect(
                IntPtr hdc,         // handle to DC
                int nLeftRect,   // x-coord of upper-left corner
                int nTopRect,    // y-coord of upper-left corner
                int nRightRect,  // x-coord of lower-right corner
                int nBottomRect  // y-coord of lower-right corner
                );

            [DllImport("Kernel32.dll")]
            public static extern int GetLastError();

            /// <summary>
            /// ScrollWindowEx
            /// </summary>
            [DllImport("user32.dll", SetLastError = true)]
            public static extern int ScrollWindowEx(
                IntPtr hWnd,
                int dx,
                int dy,
                ref  Rectangle prcScroll,
                ref Rectangle prcClip,
                IntPtr hrgnUpdate,
                ref Rectangle prcUpdate,
                int flags
                );

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool ScrollWindow(
                IntPtr hWnd,
                int dx,
                int dy,
                ref Rectangle pRect,
                ref Rectangle pClipRect
                );

            [DllImport("gdi32.dll", SetLastError = true)]
            public static extern uint RealizePalette(
             HDC hdc   // handle to DC
            );

            [DllImport("gdi32.dll", SetLastError = true)]
            public static extern bool StretchBlt(
                  IntPtr hdcDest,      // handle to destination DC
                  int nXOriginDest, // x-coord of destination upper-left corner
                  int nYOriginDest, // y-coord of destination upper-left corner
                  int nWidthDest,   // width of destination rectangle
                  int nHeightDest,  // height of destination rectangle
                  IntPtr hdcSrc,       // handle to source DC
                  int nXOriginSrc,  // x-coord of source upper-left corner
                  int nYOriginSrc,  // y-coord of source upper-left corner
                  int nWidthSrc,    // width of source rectangle
                  int nHeightSrc,   // height of source rectangle
                  TernaryRasterOperations dwRop
                );

            /// <summary>
            /// StretchBlt in windows GDI
            /// </summary>
            /// <param name="hdc"></param>
            /// <param name="nStretchMode"></param>
            /// <returns></returns>
            [DllImport("gdi32.dll", SetLastError = true)]
            public static extern int SetStretchBltMode(
                IntPtr hdc,
                StretchBltMode nStretchMode
                );

            ///<summary>
            ///GetBitmapDimensionEx in Windows GDI
            ///</summary>
            [DllImport("gdi32.dll", SetLastError = true)]
            public static extern bool GetBitmapDimensionEx(
                IntPtr hBitmap,
                out Size dimension
                );

            //[DllImport("gdi32.dll", SetLastError = true)]
            //unsafe private static extern int GetObject(
            //    IntPtr hGdiObj,
            //    int nBuffer,
            //    void* lpvObject
            //    );

            /// <summary>
            /// GetBitmapSize
            /// This method return the size of a bitmap as given the hBitmap
            /// </summary>
            /// <param name="hBitmap"></param>
            /// <returns></returns>
            //public static Size GetBitmapSize(IntPtr hBitmap)
            //{
            //    unsafe
            //    {
            //        BITMAP bmp = new BITMAP();
            //        GetObject(hBitmap, 48, (void*)&bmp);
            //        return new Size(bmp.bmWidth, bmp.bmHeight);
            //    }
            //}

            //private unsafe struct BITMAP
            //{
            //    int bmType;
            //    public int bmWidth;
            //    public int bmHeight;
            //    int bmWidthBytes;
            //    short bmPlanes;
            //    short  bmBitsPixel;
            //    void* bmBits; 
            //};

            ///<summary>
            ///GetClientRect in Windows APIs
            ///</summary>
            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool GetClientRect(
                IntPtr hWnd,
                out Rectangle rectClient
                );

            [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
            public static extern HPalette SelectPalette
            (
              HDC hdc,                // handle to DC
              HPalette hpal,          // handle to logical palette
              bool bForceBackground   // foreground or background mode
            );

            /// <summary>
            /// Enumeration for the raster operations used in StretchBlt.
            /// In C++ these are actually #define. But to use these
            /// constants with C#, a new enumeration type is defined.
            /// </summary>
            public enum StretchBltMode
            {
                BLACKONWHITE = 1,
                WHITEONBLACK = 2,
                COLORONCOLOR = 3,
                HALFTONE = 4,
                MAXSTRETCHBLTMODE = 4
            }
        }
    }
}
