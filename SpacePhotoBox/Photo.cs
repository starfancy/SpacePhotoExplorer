using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace StudioFancy.SpacePhotoBox
{
    abstract class Photo
    {
        protected Bitmap _bitmap;
        protected IntPtr _hBitmap;
        protected Point _position= Point.Empty;
        protected double _zoomRatio = 1.0;
        protected int _photoNum;
        protected int _width;
        protected int _height;

        public abstract Bitmap GetBitmap();
        public abstract IntPtr GetHBitmap();
        abstract public void Dispose();

        public int Width
        {
            get { return (int)(_width * _zoomRatio); }
        }

        public int Height
        {
            get { return (int)(_height * _zoomRatio); }
        }

        public Size Size
        {
            get { return new Size(Width, Height); }
        }

        public int UnZoomWidth
        {
            get { return _width; }
        }
        public int UnZoomHeight
        {
            get { return _height; }
        }

        public Size ActualSize
        {
            get { return new Size(_width, _height); }
        }

        public double ZoomRatio
        {
            get { return _zoomRatio; }
        }

        public int Number
        {
            get { return _photoNum; }
            set { _photoNum = value; }
        }
            
        public Point PositionInCanvas
        {
            get { return _position; }
            set { _position = value; }
        }

        public void FitPhotoToCanvas(Size canvasSize)
        {
            if (canvasSize == Size.Empty)
                return;
            if (_width > canvasSize.Width)
                _zoomRatio = canvasSize.Width / (double)_width;
        }
    }

    class ManagedPhoto : Photo
    { 
         public ManagedPhoto(Bitmap bitmap)
        {
            _bitmap = bitmap;
            _width = bitmap.Width;
            _height = bitmap.Height;
        }

         public ManagedPhoto(Bitmap bitmap, Size canvasSize)
            : this(bitmap)
        {
            FitPhotoToCanvas(canvasSize);
        }

         override public Bitmap GetBitmap()
         {
             return _bitmap;
         }

         public override IntPtr GetHBitmap()
         {
             throw new NotSupportedException("Native bitmap is not supported by this class.");
         }

         override public void Dispose()
         {
             _bitmap.Dispose();
         }
    }

    class NativePhoto : Photo
    {
        public NativePhoto(IntPtr hBitmap, int width, int height)
        {
            _hBitmap = hBitmap;
            _width = width;
            _height = height;
        }

        public NativePhoto(IntPtr hBitmap, int width, int height, Size canvasSize)
            : this(hBitmap, width, height)
        {
            FitPhotoToCanvas(canvasSize);
        }

        override public Bitmap GetBitmap()
        {
            throw new NotSupportedException("Managed bitmap is not supported by this class.");
        }

        public override IntPtr GetHBitmap()
        {
            return _hBitmap;
        }

        override public void Dispose()
        {
            Win32GDISupport.DeleteObject(_hBitmap);
        }
    }
}
