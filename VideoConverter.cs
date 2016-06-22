﻿using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Microsoft.Samples.Kinect.RecordAndPlaybackBasics
{

    public sealed class VideoConverter
    {
        private List<Image<Bgr, byte>> video = new List<Image<Bgr, byte>>();
        public VideoConverter()
        {

        }

        public List<Image<Bgr, byte>> ColorViewToAVI(WriteableBitmap ColorBitmap)
        {
            Bitmap bmp;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create((BitmapSource)ColorBitmap));
                enc.Save(outStream);
                bmp = new System.Drawing.Bitmap(outStream);
            }
            Image<Bgr, byte> img = new Image<Bgr, byte>(bmp);
            video.Add(img);
            return video;
        }

        public List<Image<Bgr, byte>> BodyViewToAVI(DrawingImage image)
        {
            Bitmap bmp;
            MemoryStream ms = new MemoryStream();
            BitmapSource temp = ToBitmapSource(image);
            var encoder = new System.Windows.Media.Imaging.BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(temp as BitmapSource));
            encoder.Save(ms);
            ms.Flush();
            bmp = new System.Drawing.Bitmap(ms);
            Image<Bgr, byte> img = new Image<Bgr, byte>(bmp);
            video.Add(img);
            return video;
        }

        public BitmapSource ToBitmapSource(DrawingImage source)
        {
            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawImage(source, new Rect(new System.Windows.Point(0, 0), new System.Windows.Size(source.Width, source.Height)));
            drawingContext.Close();

            RenderTargetBitmap bmp = new RenderTargetBitmap((int)source.Width, (int)source.Height, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(drawingVisual);
            return bmp;
        }


    }
}
