using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using System.IO;
using Android.Hardware;

namespace Tesseract.Test.Droid
{
    public abstract class Converter
    {


        public virtual byte[] ConvertYuvToJpeg(byte[] yuvData, Android.Hardware.Camera camera)
        {
            var cameraParameters = camera.GetParameters();
            var width = cameraParameters.PreviewSize.Width;
            var height = cameraParameters.PreviewSize.Height;
            var yuv = new YuvImage(yuvData, cameraParameters.PreviewFormat, width, height, null);
            var ms = new MemoryStream();
            var quality = 80;   // adjust this as needed
            yuv.CompressToJpeg(new Rect(0, 0, width, height), quality, ms);
            var jpegData = ms.ToArray();

            return jpegData;
        }
    }

    public class LedSegConverter : Converter
    {

        public override byte[] ConvertYuvToJpeg(byte[] yuvData, Android.Hardware.Camera camera)
        {
            return base.ConvertYuvToJpeg(yuvData, camera);
        }

    }


}