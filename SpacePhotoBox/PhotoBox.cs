#define USE_GDI

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

namespace StudioFancy.SpacePhotoBox
{

    public partial class PhotoBox : UserControl
    {
        // const defines
        const int HidePhotoCount = 1;
        const int PhotoInterval = 30;

        LinkedList<Photo> _photos = new LinkedList<Photo>();
        //bool inverse = false;

        // Navigator
        PhotoNavigator _navigator;
        int _prevHideStartNum;
        int _shownStartNum;
        int _nextHideStartNum;

        //  Offscreen bitmap 
        Point _usingBitmapPosition;
        bool _isBitmapUpdated = false;
        bool _canChangeBitmap = true;
        Size _changeBitmapOffset;
#if USE_GDI
        // GDI tool
        IntPtr _hFirstMemDc = IntPtr.Zero;
        IntPtr _hSecondMemDc = IntPtr.Zero;
        IntPtr _hUsingMemDc = IntPtr.Zero;
        Size _usingBitmapSize = Size.Empty;
        Size _firstBitmapSize = Size.Empty;
        Size _secondBitmapSize = Size.Empty;
#else
        // GDI plus tools
        Image _usingMemBitmap;
        Image _firstMemBitmap;
        Image _secondMemBitmap;
#endif
        // Control events variables
        bool _isDragging;
        Point _prevMousePosition;
        Point _curMousePosition;
        bool _canEraseBkgd = true;

        // Affiliated thread
        Thread _layoutThread;
        enum LayoutThreadState { Waiting, Working, Stop} ;
        LayoutThreadState _layoutThreadState = LayoutThreadState.Waiting;
        const int LayoutInterval = 10; // in ms
        // Indicate if photo list should be updated
        ManualResetEvent _photoListUpdateEvent;
        //// Indicate if offscreen bitmap should change
        //ManualResetEvent _bitmapUpdatedEvent;
        

        // performance measurement
        Size _scrollOffset;
        int _scrollCount;
        Stopwatch _scrollWatch = new Stopwatch();

        // drawing tools
        Brush _bkgdBrush = Brushes.Black;
       
        // zoom settings
        Size _canvasSize ;//  set to width of screen when load

        public PhotoBox()
        {
            InitializeComponent();
            _navigator = new PhotoNavigator();
            //ResetPhotoBox();
        }

        private void UpdatePhotoPosition(Size movingDifference)
        {
            lock (_photos)
            {
                foreach (Photo photo in _photos)
                {
                    photo.PositionInCanvas += movingDifference;
                }
                _usingBitmapPosition += movingDifference;
            }
        }

        private void ResetPhotoIndex()
        {
            // Reset photo index
            _prevHideStartNum = -1;
            _shownStartNum = 0;
            _nextHideStartNum = -1;
        }

        private void ResetPhotoBox()
        {
            if (_layoutThread != null)
            {
                StopLayoutThread();
                _layoutThread = null;
            }
            
            if(_photos !=null)
                if (_photos.Count != 0)
                {
                    LinkedListNode<Photo> node = _photos.First;
                    while (node != null)
                    {
                        node.Value.Dispose();
                        node = node.Next;
                    }
                    _photos.Clear();
                }
            _usingBitmapPosition = Point.Empty;

#if USE_GDI
            _hUsingMemDc = IntPtr.Zero;
#else
            _usingMemBitmap = null;
#endif

            ResetPhotoIndex();
        }

        private bool IsPhotoHide(Photo photo)
        {
            Size clientSize = this.ClientSize;
            int left = photo.PositionInCanvas.X;
            int right = left + photo.Width;
            int top = photo.PositionInCanvas.Y;
            int bottom = top + photo.Height;
            return right < 0 || left > clientSize.Width 
                || bottom < 0 || top > clientSize.Height;
        }

        public void OpenPhotoByPath(string path)
        {
            ResetPhotoBox();
            // Load photos from files into memory
            LoadPhotos(path);
            // Calc the starting position of photos
            LayoutPhotos();
#if USE_GDI
            // Draw a offscreen bitmap in GDI
            InitializeGDI();
            CreateOffScreenBitmap(ref _hFirstMemDc);
            _hUsingMemDc = _hFirstMemDc;
            _usingBitmapSize = _firstBitmapSize;
#else
            // Draw a off screen bitmap 
            CreateOffScreenBitmap(ref _firstMemBitmap);
            _usingMemBitmap = _firstMemBitmap;
#endif
            // Start layout thread
            StartLayoutThread();
            // Draw the client rect of control
            Invalidate();
        }

#if USE_GDI
        public void InitializeGDI()
        {
            if (_hFirstMemDc == IntPtr.Zero || _hSecondMemDc == IntPtr.Zero)
            {
                Graphics g = this.CreateGraphics();
                IntPtr hDc = g.GetHdc();
                if (_hFirstMemDc == IntPtr.Zero)
                    _hFirstMemDc = Win32GDISupport.CreateCompatibleDC(hDc);
                if (_hSecondMemDc == IntPtr.Zero)
                    _hSecondMemDc = Win32GDISupport.CreateCompatibleDC(hDc);
                g.ReleaseHdc();
            }
        }

        public void FinalizeGDI()
        {
            if (_hFirstMemDc != IntPtr.Zero)
                Win32GDISupport.DeleteDC(_hFirstMemDc);
            if (_hSecondMemDc != IntPtr.Zero)
                Win32GDISupport.DeleteDC(_hSecondMemDc);
        }
#endif

        private void LoadPhotos(string path)
        {   
            // Get photo file list in path
            _navigator.UpdatePhotoListInDiretory(path);
            // Load the showing photos
            Photo photo = _navigator.GetFirstPhoto();
            photo.FitPhotoToCanvas(_canvasSize);
            _shownStartNum = photo.Number;
            int clientHeight = this.ClientSize.Height;
            int photoHeight = 0;
            _photos.AddLast(photo);
            while ((photoHeight += photo.Height + PhotoInterval) < clientHeight)
            {
                photo = _navigator.GetNextPhoto();
                if (photo == null)
                    break;
                photo.FitPhotoToCanvas(_canvasSize);
                _photos.AddLast(photo);
            };
            // Load the hide photos
            // No.-1 photo does not exist, then neglect it
            _prevHideStartNum = -1;
            for (int i = 0; i < HidePhotoCount; i++)
            {
                photo = _navigator.GetNextPhoto();
                photo.FitPhotoToCanvas(_canvasSize);
                _photos.AddLast(photo);
                if (i == 0)
                    _nextHideStartNum = photo.Number;
            }
        }

        private void LayoutPhotos()
        {
            if (_photos.Count == 0)
                return;

            // Layout all the photo from (0,0)
            LinkedListNode<Photo> node = _photos.First;
            Photo photo;
            Point position = new Point(0,0);
            Point firstShowPhotoPosition = new Point(0, 0);
            do
            {
                photo = node.Value;
                photo.PositionInCanvas = position ;
                // Mark the first shown photo position
                if (photo.Number == _shownStartNum)
                    firstShowPhotoPosition = position;
                position += new Size(0, photo.Height+ PhotoInterval);
            } while ((node = node.Next) != null);
            // Move all the photo according to the first shown photo position
            if (firstShowPhotoPosition.X == 0 && firstShowPhotoPosition.Y == 0)
                return;
            node = _photos.First;
            _usingBitmapPosition.X = -firstShowPhotoPosition.X;
            _usingBitmapPosition.Y = -firstShowPhotoPosition.Y;
            do
            {
                photo = node.Value;
                photo.PositionInCanvas += new Size(_usingBitmapPosition);
            } while ((node = node.Next) != null);
        }

        private void CreateOffScreenBitmap(ref Image bitmap)
        {
            if (bitmap != null)
                bitmap.Dispose();
            int bitmapWidth = 0, bitmapHeight = 0;

            //Caculate the cached bitmap size
            LinkedListNode<Photo> node = _photos.First;
            Photo photo;
            do
            {
                photo = node.Value;
                bitmapWidth = System.Math.Max(bitmapWidth, photo.Width);
                bitmapHeight +=photo.Height + PhotoInterval;
            } while ((node = node.Next) != null);

            // Create bimap
            bitmap = new Bitmap(bitmapWidth, bitmapHeight);
            Graphics g = Graphics.FromImage(bitmap);
            g.FillRectangle(_bkgdBrush, 0, 0, bitmap.Width, bitmap.Height);

            node = _photos.First;
            Rectangle rectPhoto = Rectangle.Empty;
            do
            {
                photo = node.Value;
                // Zoom image to fit the screen width
                rectPhoto.Size = photo.Size;
                g.DrawImage(photo.GetBitmap(), rectPhoto); // DrawImage(Image, int, int) will use dpi in image file, so don't use it
                rectPhoto.Y += rectPhoto.Height + PhotoInterval;
            } while ((node = node.Next) != null);
            g.Dispose();
            //GC.Collect();
        }

#if USE_GDI
        private void CreateOffScreenBitmap(ref IntPtr hMemDc)
        {
            int bitmapWidth = 0, bitmapHeight = 0;
            //Caculate the cached bitmap size
            LinkedListNode<Photo> node = _photos.First;
            Photo photo;
            do
            {
                photo = node.Value;
                bitmapWidth = System.Math.Max(bitmapWidth, photo.Width);
                bitmapHeight += photo.Height + PhotoInterval;
            } while ((node = node.Next) != null);

            if (hMemDc == _hFirstMemDc)
            {
                _firstBitmapSize.Width = bitmapWidth;
                _firstBitmapSize.Height = bitmapHeight;
            }
            else
            {
                _secondBitmapSize.Width = bitmapWidth;
                _secondBitmapSize.Height = bitmapHeight;
            }
            // Create bimap
            Graphics g = this.CreateGraphics();
            IntPtr hDc = g.GetHdc();
            IntPtr hBitmap = Win32GDISupport.CreateCompatibleBitmap(hDc, bitmapWidth, bitmapHeight);
            IntPtr hOldBimap = Win32GDISupport.SelectObject(hMemDc, hBitmap);
            if (hOldBimap != IntPtr.Zero)
                Win32GDISupport.DeleteObject(hOldBimap);
            IntPtr hPhotoDc = Win32GDISupport.CreateCompatibleDC(hDc);
            g.ReleaseHdc();
            //g.FillRectangle(_bkgdBrush, 0, 0, bitmap.Width, bitmap.Height);

            Graphics gMem = Graphics.FromHdc(hMemDc);

            node = _photos.First;
            Rectangle rectPhoto = Rectangle.Empty;
            PointF textLocation = new PointF(100, 100);
            do
            {
                photo = node.Value;
                rectPhoto.Size = photo.Size;
                IntPtr hPhoto;
                if (photo is ManagedPhoto)
                    hPhoto = photo.GetBitmap().GetHbitmap();
                else if (photo is NativePhoto)
                    hPhoto = photo.GetHBitmap();
                else
                    throw new NotSupportedException("Native bitmap is not supported by the photo class");
                Win32GDISupport.SelectObject(hPhotoDc, hPhoto);
                Win32GDISupport.SetStretchBltMode(hMemDc, Win32GDISupport.StretchBltMode.HALFTONE);
                Win32GDISupport.StretchBlt(hMemDc, rectPhoto.X, rectPhoto.Y, rectPhoto.Width, rectPhoto.Height,
                    hPhotoDc, 0, 0, photo.UnZoomWidth, photo.UnZoomHeight, Win32GDISupport.TernaryRasterOperations.SRCCOPY);
                if (photo is ManagedPhoto)
                    Win32GDISupport.DeleteObject(hPhoto);

                //gMem.DrawImage(photo.Bitmap, rectPhoto);
                gMem.DrawString(photo.Number.ToString(), SystemFonts.DefaultFont, Brushes.White, textLocation);
                textLocation.Y += rectPhoto.Height + PhotoInterval;
                rectPhoto.Y += rectPhoto.Height + PhotoInterval;

            } while ((node = node.Next) != null);
            Win32GDISupport.DeleteDC(hPhotoDc);
            gMem.Dispose();
            Trace.WriteLine("++++++Bitmap created, photo number "+ _photos.First().Number+ 
                    " to " + _photos.Last().Number );
            GC.Collect();
        }
#endif

        // update the photo show status pointers
        // return true if there is at least one pointer has changed,
        // return false otherwise.
        private bool UpdatePhotoShowPointers()
        {
            // check if control is unseen
            if (this.Width == 0 || this.Height == 0)
                return false;
            lock (_photos)
            {
                int oldPrevHideStartNum = _prevHideStartNum;
                int oldShowStartNum = _shownStartNum;
                int oldNextHideStartNum = _nextHideStartNum;

                // remark the pointers
                ResetPhotoIndex();
                bool prevHide = false;
                bool thisHide = false;
                if (IsPhotoHide(_photos.First()))
                {
                    _prevHideStartNum = _photos.First().Number;
                    prevHide = true;
                }
                LinkedListNode<Photo> node = _photos.First.Next;
                while (node != null)
                {
                    thisHide = IsPhotoHide(node.Value);
                    if (prevHide != thisHide)
                    {
                        if (thisHide == false)
                            _shownStartNum = node.Value.Number;
                        else
                            _nextHideStartNum = node.Value.Number;
                    }
                    prevHide = thisHide;
                    node = node.Next;
                }
                Debug.Assert(_shownStartNum > _prevHideStartNum);
                if (_nextHideStartNum != -1)
                    Debug.Assert(_nextHideStartNum > _shownStartNum);

                Trace.WriteLine("prev : "+ _prevHideStartNum + 
                    " show: "+ _shownStartNum + " next: "+ _nextHideStartNum);

                return oldPrevHideStartNum != _prevHideStartNum ||
                        oldShowStartNum != _shownStartNum ||
                        oldNextHideStartNum != _nextHideStartNum;
            }
        }

        bool PhotoListNeedUpdate()
        {
            int prevHideCount;
            if (_prevHideStartNum == -1)
                prevHideCount = 0;
            else
                prevHideCount = _shownStartNum - _prevHideStartNum;

            int nextHideCount;
            if (_nextHideStartNum == -1)
                nextHideCount = 0;
            else
                nextHideCount = _photos.Last().Number - _nextHideStartNum + 1;

            return (prevHideCount < HidePhotoCount && _photos.First().Number >0)
                || prevHideCount > HidePhotoCount
                || (nextHideCount < HidePhotoCount && _photos.Last().Number < _navigator.Count-1)
                || nextHideCount > HidePhotoCount;
        }

        private void ChangeBitmap()
        {
            string bitmapName;
#if USE_GDI
            if (_hUsingMemDc == _hFirstMemDc)
            {
                _hUsingMemDc = _hSecondMemDc;
                _usingBitmapSize = _secondBitmapSize;
                bitmapName = "2nd bitmap";
            }
            else
            {
                _hUsingMemDc = _hFirstMemDc;
                _usingBitmapSize = _firstBitmapSize;
                bitmapName = "1st bitmap";
            }
#else
            if (_usingMemBitmap == _firstMemBitmap)
            {
                _usingMemBitmap = _secondMemBitmap;
            }
            else
            {
                _usingMemBitmap = _firstMemBitmap;
            }
#endif
            lock (_photos)
            {
                _usingBitmapPosition += _changeBitmapOffset;
                Trace.WriteLine("-------Changed offscreen bitmap to " + bitmapName + " . Position: " +
                        _usingBitmapPosition.ToString() + " Offset: " + _changeBitmapOffset.ToString());
                _changeBitmapOffset = Size.Empty;
            }
            _isBitmapUpdated = false;
            _canEraseBkgd = false;
            Invalidate();
        }

        private void ScrollPhoto(int offsetX, int offsetY)
        {
            Rectangle clientRect = ClientRectangle;
            // check if the moved bitmap will exceed the confine of client area
            int x = _usingBitmapPosition.X, y = _usingBitmapPosition.Y;
            int bitmapWidth, bitmapHeight;
#if USE_GDI
            bitmapWidth = _usingBitmapSize.Width;
            bitmapHeight = _usingBitmapSize.Height;
#else
            bitmapWidth = _usingMemBitmap.Width;
            bitmapHeight = _usingMemBitmap.Height;
#endif
            if (bitmapWidth > clientRect.Width)
            {
                x = _usingBitmapPosition.X + offsetX;
                if (x < clientRect.Width - bitmapWidth)
                    x = clientRect.Width - bitmapWidth;
                else if (x > 0)
                    x = 0;
            }
            if (bitmapHeight > clientRect.Height)
            {
                y = _usingBitmapPosition.Y+ offsetY;
                if (y < clientRect.Height - bitmapHeight)
                    y = clientRect.Height - bitmapHeight;
                else if (y > 0)
                    y = 0;
            }
            offsetX = x - _usingBitmapPosition.X;
            offsetY = y - _usingBitmapPosition.Y;
            if (offsetX == 0 && offsetY == 0)
                return;

            Win32GDISupport.ScrollWindow(this.Handle, offsetX, offsetY, ref clientRect, ref clientRect);
            _canEraseBkgd = false;

            UpdatePhotoPosition(new Size(offsetX, offsetY));
            // if photo list should be updated, then set this event
            UpdatePhotoShowPointers();
            if (PhotoListNeedUpdate() && !_isBitmapUpdated)
                _photoListUpdateEvent.Set();

            //// Performance measurement
            //_scrollOffset.Width += offsetX;
            //_scrollOffset.Height += offsetY;
            //_scrollCount++;
            //int milSeconds = _scrollWatch.Elapsed.Milliseconds;
            //if (milSeconds > 0)
            //{
            //    StringBuilder sb = new StringBuilder();
            //    sb.AppendFormat("Scroll: X-{0} pix/s Y-{1} pix/s {2} times/s", offsetX * 1000 / milSeconds,
            //        offsetY * 1000 / milSeconds, _scrollCount * 1000 / milSeconds);
            //    Trace.WriteLine(sb.ToString());
            //}
        }
        
        // Layout thread----------------------------------------------------------------------------
        private void StartLayoutThread()
        {
            if (_photoListUpdateEvent == null)
                _photoListUpdateEvent = new ManualResetEvent(false);

            _layoutThreadState = LayoutThreadState.Working;
            _layoutThread = new Thread(new ThreadStart(LayoutThreadMain));
            _layoutThread.Name = "Layout Thread";
            _layoutThread.Priority = ThreadPriority.Lowest;
            _layoutThread.Start();
        }

        private void StopLayoutThread()
        {
            if (_layoutThread != null)
            {
                _photoListUpdateEvent.Set();
                
                _layoutThreadState = LayoutThreadState.Stop;
                Trace.WriteLine("Waiting for layout thread stop.");
                _layoutThread.Join();
                _layoutThread = null;

                _photoListUpdateEvent.Close();
                _photoListUpdateEvent = null;
            }
        }
        
        private void LayoutThreadMain()
        {
            while (_layoutThreadState != LayoutThreadState.Stop)
            {
                switch (_layoutThreadState)
                {
                    case LayoutThreadState.Working:
                        Trace.WriteLine("++++++++Layout thread- waiting for photoListUpdateEvent.......");
                        _photoListUpdateEvent.WaitOne();
                        //_photoListUpdateEvent.Reset();
                        Trace.WriteLine("++++++++Layout thread- waiting for photoListUpdateEvent succeed.");
                        //if (!_canChangeBitmap)
                        //    break;
                        // Layout thread is working then photo list should not be empty
                        Debug.Assert(_photos.Count != 0);
                        if (UpdatePhotoList() && !_isBitmapUpdated)
                        {
#if USE_GDI
                            if (_hUsingMemDc == _hFirstMemDc)
                            {
                                CreateOffScreenBitmap(ref _hSecondMemDc);
                                Trace.WriteLine("++++++Updated 2nd bitmap. " + " Size is " + _secondBitmapSize);
                            }
                            else
                            {
                                CreateOffScreenBitmap(ref _hFirstMemDc);
                                Trace.WriteLine("++++++Updated 1st bitmap.  " + " Size is " + _firstBitmapSize);
                            }
#else
                                if (_usingMemBitmap == _firstMemBitmap)
                                {
                                    CreateOffScreenBitmap(ref _secondMemBitmap);
                                    _secondBitmapOffset = new Size(_photos.First().PositionInCanvas);
                                    Trace.WriteLine("Updated 2nd bitmap. Offset is " + _secondBitmapOffset.ToString() 
                                        +" Size is "+ _secondMemBitmap.Size );
                                }
                                else
                                {
                                    CreateOffScreenBitmap(ref _firstMemBitmap);
                                    _firstBitmapOffset = new Size(_photos.First().PositionInCanvas);
                                    Trace.WriteLine("Updated 1st bitmap. Offset is " + _firstBitmapOffset.ToString()
                                        + " Size is " + _firstMemBitmap.Size);
                                }
#endif
                            _photoListUpdateEvent.Reset();
                            _isBitmapUpdated = true;
                        }
                        break;
                    case  LayoutThreadState.Waiting:
                        Thread.Sleep(LayoutInterval);
                        break;
                    default:
                        Thread.Sleep(LayoutInterval);
                        break;
                }
            }
            Trace.WriteLine("Layout thread exits.");
        }

        // remove the unnecessary hide cached photo
        // and insert the extra cached photo in hide position
        // return true if Photo list has been updated, 
        // return false otherwise.
        private bool UpdatePhotoList()
        {
            Debug.Assert(_photos.Count > 0);
            bool updated = false;
            Size bitmapOffset = Size.Empty;

            // Remove or add prev hide photo
            int prevHideCount;
            if (_prevHideStartNum == -1)
                prevHideCount = 0;
            else
                prevHideCount = _shownStartNum - _prevHideStartNum;
            // Remove the extra photo cached
            if (prevHideCount > HidePhotoCount)
            {
                int count = prevHideCount - HidePhotoCount;
                while (count-- > 0)
                {
                    bitmapOffset.Height += _photos.First().Height + PhotoInterval;
                    _photos.First().Dispose();
                    lock (_photos)
                    {
                        _photos.RemoveFirst();
                    }
                }
                updated = true;
            }
            // Add the neccessary photo to be cached
            else if (prevHideCount < HidePhotoCount)
            {
                int count = HidePhotoCount - prevHideCount;
                _navigator.Seek(_photos.First().Number);
                while (count-- > 0)
                {
                    Photo photo = _navigator.GetPrevPhoto();
                    if (photo == null)
                        break;
                    photo.FitPhotoToCanvas(_canvasSize);
                    lock (_photos)
                    {
                        photo.PositionInCanvas = new Point(_photos.First().PositionInCanvas.X,
                            _photos.First().PositionInCanvas.Y - photo.Height - PhotoInterval);
                        _photos.AddFirst(photo);
                    }
                    bitmapOffset.Height -= _photos.First().Height + PhotoInterval;
                    updated = true;
                }
            }

            // Remove or add next hide photo
            int nextHideCount;
            if (_nextHideStartNum == -1)
                nextHideCount = 0;
            else
                nextHideCount = _photos.Last().Number - _nextHideStartNum + 1;
            // Remove the extra photo cached
            if (nextHideCount > HidePhotoCount)
            {
                int count = nextHideCount - HidePhotoCount;
                while (count-- > 0)
                {
                    _photos.Last().Dispose();
                    lock (_photos)
                        _photos.RemoveLast();
                }
                updated = true;
            }
            // Add the neccessary photo to be cached
            else if (nextHideCount < HidePhotoCount)
            {
                int count = HidePhotoCount - nextHideCount;
                _navigator.Seek(_photos.Last().Number);
                while (count-- > 0)
                {
                    Photo photo = _navigator.GetNextPhoto();
                    if (photo == null)
                        break;
                    photo.FitPhotoToCanvas(_canvasSize);
                    lock (_photos)
                    {
                        photo.PositionInCanvas = new Point(_photos.Last().PositionInCanvas.X,
                                _photos.Last().PositionInCanvas.Y + _photos.Last().Height + PhotoInterval);
                        _photos.AddLast(photo);
                    }
                    updated = true;
                }
            }
            lock (_photos)
                _changeBitmapOffset += bitmapOffset;

            if (updated)
                Trace.WriteLine("+++++++Photo list updated.  Photo number is " + _photos.First().Number + " to "
                    + _photos.Last().Number + " bitmap offset: " + _changeBitmapOffset);
            return updated;
        }

        // Control events----------------------------------------------------------------------------
        private void PhotoBox_Paint(object sender, PaintEventArgs e)
        {
#if USE_GDI
            if (_hUsingMemDc == IntPtr.Zero)
                return;
#else
           if( _usingMemBitmap == null )
                return;
#endif
            Graphics gScreen = this.CreateGraphics();
#if USE_GDI
            IntPtr hDc = gScreen.GetHdc();
            Win32GDISupport.BitBlt(hDc, _usingBitmapPosition.X, _usingBitmapPosition.Y, _usingBitmapSize.Width, _usingBitmapSize.Height,
                _hUsingMemDc, 0, 0, Win32GDISupport.TernaryRasterOperations.SRCCOPY);
            //int result =Win32GDISupport.GetLastError();
            //Trace.WriteLine("Area repainted... result: " + result + " Pos: " + _usingBitmapPosition.ToString() + " Size: " + _usingBitmapSize.ToString());
            gScreen.ReleaseHdc();
#else
            gScreen.DrawImage(_usingMemBitmap, _usingBitmapOffset.Width, _usingBitmapOffset.Height);
#endif
            gScreen.Dispose();
            _canEraseBkgd = true;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (_canEraseBkgd)
                base.OnPaintBackground(e);
        }       

        private void PhotoBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {               
#if USE_GDI
                if (_hUsingMemDc != IntPtr.Zero)
#else
                if(_usingMemBitmap !=null)
#endif
                {
                    _curMousePosition = e.Location;
                    int offsetX = _curMousePosition.X - _prevMousePosition.X;
                    int offsetY = _curMousePosition.Y - _prevMousePosition.Y;
                    ScrollPhoto(offsetX, offsetY);
                }
            }
            _prevMousePosition = e.Location;     
        }

        private void PhotoBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
#if USE_GDI
                if (_hUsingMemDc != IntPtr.Zero)
#else
                if(_usingMemBitmap !=null)
#endif
                {
                    _isDragging = false;
                    this.Capture = false;
                    _canChangeBitmap = true;

                    _scrollWatch.Reset();
                }
            }
        }

        private void PhotoBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left )
            {
#if USE_GDI
                if (_hUsingMemDc != IntPtr.Zero)
#else
                if(_usingMemBitmap !=null)
#endif
                {
                    _isDragging = true;
                    this.Capture = true;
                    _canChangeBitmap = false;
                    // Scroll performance mesurement
                    _scrollOffset = Size.Empty;
                    _scrollCount = 0;
                    _scrollWatch.Start();
                }
            }
        }

        private void PhotoBox_Load(object sender, EventArgs e)
        {
            // Only Form class has the Close event, so call the close handler when main form is closing
            Form parent = this.Parent as Form;
            Debug.Assert(parent !=null);
            parent.FormClosing += new FormClosingEventHandler(PhotoBox_Close);
            parent.MouseWheel += new MouseEventHandler(PhotoBox_MouseWheel);

            _canvasSize.Width = (int)(Screen.PrimaryScreen.Bounds.Width*1.2);
        }

        private void PhotoBox_Close(object sender, EventArgs e)
        {
            StopLayoutThread();
        }

        private void bitmapTimer_Tick(object sender, EventArgs e)
        {
            if (_canChangeBitmap && _isBitmapUpdated)
                ChangeBitmap();
        }

        private void PhotoBox_MouseWheel(object sender, MouseEventArgs e)
        {
            int offsetY = e.Delta;
            if (offsetY != 0)
            {
                if (offsetY > 100)
                    offsetY = 100;
                else if (offsetY < -100)
                    offsetY = -100;
                ScrollPhoto(0, offsetY);
            }
        }
    }
}
