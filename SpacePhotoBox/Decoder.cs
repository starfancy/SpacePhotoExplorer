using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace StudioFancy.SpacePhotoBox
{
    abstract class Decoder
    {
        abstract public Photo Decode(string file);
    }

    class ManagedDecoder : Decoder
    {
        override public Photo Decode(string file)
        {
            Image image = Image.FromFile(file);
            Photo photo = new ManagedPhoto((Bitmap)image);
            return photo;
        }
    }
}
