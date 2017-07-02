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
using System.Drawing;


namespace Tesseract.Test.Droid
{
    public class Converter
    {
        public Bitmap ToTesseract { get; private set; }
        public Bitmap ToInterpret { get; private set; }

        private static int dr = 45;
        private static int dbX = -400;
        private static int dbY = -200;

        public virtual RectangleF sorceClip(Size cameraParameter)
        {
          //  return  new RectangleF(0, 0, cameraParameter.Width, cameraParameter.Height / 3);
            return  new RectangleF(0, 0, cameraParameter.Height, cameraParameter.Width / 3);
        }

        public virtual Rect sorceClipRect(Size cameraParameter)
        {
            var src = sorceClip(cameraParameter);
            return new Rect((int)src.Left, (int)src.Top, (int)src.Right, (int)src.Bottom);
        }



        public virtual byte[] ConvertYuvToJpeg(byte[] yuvData,ImageFormatType cameraFotmat, Size previewSize , Size centerClip  )
        {
            
            var yuv = new YuvImage(yuvData, cameraFotmat, previewSize.Width, previewSize.Height, null);
            byte[] jpegData;
            using (var ms = new MemoryStream())
            {
                var quality = 100;
                // yuv.CompressToJpeg(sorceClipRect(cameraParameter), quality, ms);
                 yuv.CompressToJpeg(new Rect(0,0,previewSize.Width,previewSize.Height), quality, ms);
                jpegData = ms.ToArray();
            }

            try
            {

                var src = sorceClip(previewSize);
                BitmapFactory.Options bitmapFatoryOptions = new BitmapFactory.Options();
                bitmapFatoryOptions.InPreferredConfig = Bitmap.Config.Rgb565;
                bitmapFatoryOptions.InMutable = true;

                //１．Bitmapの生成時にオプションを与えてmutableなオブジェクトを生成する。
                //http://dev.classmethod.jp/smartphone/android/android-immutable-bitmap-mutable/
                var srcbmp = BitmapFactory.DecodeByteArray(jpegData, 0, jpegData.Length, bitmapFatoryOptions);
                var m = new Matrix();
                //   m.PostTranslate(-1 * previewSize.Width, -1 * previewSize.Height / 3);
                   m.PostRotate(90);
             //   m.PostRotate(dr);
                 m.PostTranslate(previewSize.Height, 0);
               // m.PostTranslate(dbX, dbY);

                //  var bmp = Bitmap.CreateBitmap(srcbmp, 0, 0, (int)src.Width, (int)src.Height, m, true);
                //  var bmp = Bitmap.CreateBitmap((int)src.Width, (int)src.Height, srcbmp.GetConfig());                       
                var bmp = Bitmap.CreateBitmap((int)src.Width, (int)src.Height, srcbmp.GetConfig());                       
                using (var cv = new Canvas(bmp))
                {
                    
                    var left = (src.Width - centerClip.Width) / 2;
                    var top = (src.Height - centerClip.Height) / 2;
                    var bottom = top + centerClip.Height;
                    var rigtht = left + centerClip.Width;
                    var p = new Paint();
                    p.Color = Android.Graphics.Color.White;
                    p.SetStyle(Paint.Style.Fill);
                    cv.DrawBitmap(srcbmp, m, p);

                    cv.DrawRect(0, 0, left, src.Bottom, p);
                    cv.DrawRect(0, 0, src.Width, top, p);
                    cv.DrawRect(0, bottom, src.Width, src.Bottom, p);
                    cv.DrawRect(rigtht, 0, src.Right, src.Bottom, p);
                }
                //ToTesseract = bmp.Copy(bmp.GetConfig(), true);
                ToInterpret = bmp.Copy(bmp.GetConfig(), true);

                using (var ms = new MemoryStream())
                {
                    var quality = 100;
                    bmp.Compress(Bitmap.CompressFormat.Jpeg, quality, ms);
                    jpegData = ms.ToArray();
                }
            }
            catch(Exception ex)
            {
                Android.Util.Log.Debug("convert", ex.ToString());
            }
            return jpegData;
        }
    }

    public class LedSegConverter : Converter
    {
        
        

    }


}