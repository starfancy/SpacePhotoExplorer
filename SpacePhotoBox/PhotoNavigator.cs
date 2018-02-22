using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace StudioFancy.SpacePhotoBox
{
    class PhotoNavigator
    {
        int _photoCount=0;
        int _photoIndex=-1;
        List<string> _photoList = new List<string>();
        Decoder _photoDecoder;

        public PhotoNavigator()
        {
            _photoDecoder = new ManagedDecoder();
        }

        public int CurrentIndex
        {
            get { return _photoIndex; }
        }

        public int Count
        {
            get { return _photoCount; }
        }

        public void UpdatePhotoListInDiretory(string path)
        {
            string[] fileList = Directory.GetFiles(path);
            _photoList.Clear();
            foreach(string file in fileList)
            {
                if (IsImageFile(file))
                    _photoList.Add(file);         
            }
            _photoCount = _photoList.Count;
            _photoIndex = 0;
        }

        public bool Seek(int index)
        {
            if (_photoIndex < 0 || _photoIndex > _photoCount - 1)
                return false;
            else
            {
                _photoIndex = index;
                return true;
            }
        }

        public Photo GetFirstPhoto()
        {
            return GetPhotoByIndex(0);
        }

        public Photo GetLastPhoto()
        {
            return GetPhotoByIndex(_photoCount-1);
        }

        public Photo GetNextPhoto()
        {
            return GetPhotoByIndex(_photoIndex + 1);
        }

        public Photo GetPrevPhoto()
        {
            return GetPhotoByIndex(_photoIndex - 1);
        }

        public Photo GetPhotoByIndex(int index)
        {
            if (index < 0 || index > _photoCount - 1)
                return null;
            _photoIndex = index;
            Photo photo = _photoDecoder.Decode( _photoList[index]);
            photo.Number = index;
            return photo;
        }

        public static bool IsImageFile(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            ext = ext.ToLower();
            switch (ext)
            {
                case ".bmp":
                case ".jpeg":
                case ".jpg":
                case ".jpe":
                case ".png":
                case ".gif":
                case "tiff":
                    return true;
                default:
                    return false;
            }
        }
    }
}
