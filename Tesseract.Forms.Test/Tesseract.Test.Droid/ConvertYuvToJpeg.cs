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
using Tesseract.Droid;
using Android.Hardware;

namespace Tesseract.Test.Droid
{
    class ConvertYuvToJpeg
    {
        protected Action<Bitmap> SaveBitmap;
        protected bool debugSwitch;

        public ConvertYuvToJpeg(Action<Bitmap> saveBitmap)
        {
            SaveBitmap = saveBitmap;
        }

        public virtual byte[] Convert(byte[] yuvData, Android.Hardware.Camera camera)
        {
            var cameraParameters = camera.GetParameters();
            var width = cameraParameters.PreviewSize.Width;
            var height = cameraParameters.PreviewSize.Height;
            var yuv = new YuvImage(yuvData, cameraParameters.PreviewFormat, width, height, null);
            var ms = new MemoryStream();
            var quality = 80;   // adjust this as needed
            yuv.CompressToJpeg(new Rect(0, 0, width, height), quality, ms);
            var jpegData = ms.ToArray();

            try
            {
                var src = BitmapFactory.DecodeByteArray(jpegData, 0, jpegData.Length, new BitmapFactory.Options { InSampleSize = 1 });
                var rotate = Bitmap.CreateBitmap(src.Height, src.Width / 3, src.GetConfig());
                var cv = new Canvas(rotate);
                Paint p = new Paint();
                p.AntiAlias = false;
                p.SetStyle(Paint.Style.Fill);
                cv.Save(SaveFlags.All);
                var delata = Math.Abs(src.Height - src.Width);
                cv.Rotate(90, src.Width / 2, src.Height / 2);
                cv.DrawBitmap(src, delata / 2, delata / 2, p);
                cv.Restore();

                p.Color = Color.White;
                var r = (int)(src.Height * 0.1);
                var r2 = (int)(src.Height * 0.3);
                cv.DrawRect(0, 0, r2, rotate.Height, p);
                cv.DrawRect(0, 0, rotate.Width, r, p);
                cv.DrawRect(rotate.Width - r2, 0, rotate.Width, rotate.Height, p);
                cv.DrawRect(0, rotate.Height - r, rotate.Width, rotate.Height, p);

                if (!debugSwitch) SaveBitmap(rotate);
                //overlay.PreprocessingPreviewFrame = rotate;

                var ms2 = new MemoryStream();
                rotate.Compress(Bitmap.CompressFormat.Jpeg, quality, ms2);
                
                return ms2.ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return null;
            }
        }

    }



    class ConvertYToJpeg : ConvertYuvToJpeg
    {
        protected int convertYLevel;

        public ConvertYToJpeg(Action<Bitmap> saveBitmap,int convertYLevel) :base(saveBitmap)
        {
            this.convertYLevel = convertYLevel;
            System.Diagnostics.Debug.WriteLine("ConvertYToJpeg convertYLevel=" + convertYLevel.ToString());
        }

        private int[] decodeYUV420SP(byte[] yuv420sp, int width, int height)
        {

            int[] res = new int[width * height];
            int frameSize = width * height;

            for (int j = 0, yp = 0; j < height; j++)
            {
                int uvp = frameSize + (j >> 1) * width, u = 0, v = 0;
                for (int i = 0; i < width; i++, yp++)
                {
                    int y = (0xff & ((int)yuv420sp[yp])) - 16;
                    if (y < 0) y = 0;
                    if ((i & 1) == 0)
                    {
                        try
                        {
                            v = (0xff & yuv420sp[uvp++]) - 128;
                            u = (0xff & yuv420sp[uvp++]) - 128;
                        }
                        catch (Exception e)
                        {

                        }
                    }
                    int y1192 = 1192 * y;
                    //int r = (y1192 + 1634 * v);
                    //int g = (y1192 - 833 * v - 400 * u);
                    //int b = (y1192 + 2066 * u);

                    int r = (y1192 + 1634 * v);
                    int g = (y1192 - 833 * v - 400 * u);
                    int b = (y1192 + 2066 * u);
                    if (convertYLevel < y)
                    {
                        r = g = b = 0;
                    }
                    else
                    {
                        r = g = b = 262143;
                    }
                    if (r < 0) r = 0; else if (r > 262143) r = 262143;
                    if (g < 0) g = 0; else if (g > 262143) g = 262143;
                    if (b < 0) b = 0; else if (b > 262143) b = 262143;
                    res[yp] = (int)(0xff000000 | ((r << 6) & 0xff0000) | ((g >> 2) & 0xff00) | ((b >> 10) & 0xff));
                }
            }
            return res;
        }

        public override byte[] Convert(byte[] yuvData, Android.Hardware.Camera camera)
        {
            var cameraParameters = camera.GetParameters();
            var width = cameraParameters.PreviewSize.Width;
            var height = cameraParameters.PreviewSize.Height;

            int[] mIntArray;// = new int[mWidth * mHeight];

            mIntArray = decodeYUV420SP(yuvData, width, height);

            Android.Graphics.Bitmap yuv = Android.Graphics.Bitmap.CreateBitmap(width, height, Android.Graphics.Bitmap.Config.Argb4444);
            yuv.SetPixels(mIntArray, 0, width, 0, 0, width, height);

            var ms = new MemoryStream();
            var quality = 80;   // adjust this as needed
            yuv.Compress(Bitmap.CompressFormat.Jpeg, quality, ms);
            var jpegData = ms.ToArray();

            try
            {
                var src = BitmapFactory.DecodeByteArray(jpegData, 0, jpegData.Length, new BitmapFactory.Options { InSampleSize = 1 });
                var rotate = Bitmap.CreateBitmap(src.Height, src.Width / 3, src.GetConfig());
                var cv = new Canvas(rotate);
                Paint p = new Paint();
                p.AntiAlias = false;
                p.SetStyle(Paint.Style.Fill);
                cv.Save(SaveFlags.All);
                var delata = Math.Abs(src.Height - src.Width);
                cv.Rotate(90, src.Width / 2, src.Height / 2);
                cv.DrawBitmap(src, delata / 2, delata / 2, p);
                cv.Restore();

                p.Color = Color.White;
                var rUp = (int)(rotate.Height * 0.3);
                var rDown = rotate.Height - rUp;
                var r2 = (int)(src.Height * 0.3);

                // 排除位置探索
                rUp = repper(rotate, rUp,r2,false);
                rDown = repper(rotate, rDown, r2,true);

                //p.Alpha = 0xC0;

                cv.DrawRect(0, 0, r2, rotate.Height, p);
                //p.Color = Color.Green;
                cv.DrawRect(0, 0, rotate.Width, rUp, p);
                cv.DrawRect(rotate.Width - r2, 0, rotate.Width, rotate.Height, p);
                //p.Color = Color.Yellow;
                cv.DrawRect(0, rDown, rotate.Width, rotate.Height, p);

                if (!debugSwitch) SaveBitmap(rotate);
                //overlay.PreprocessingPreviewFrame = rotate;

                var ms2 = new MemoryStream();
                rotate.Compress(Bitmap.CompressFormat.Jpeg, quality, ms2);

                return ms2.ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// 水平方向1ライン上に黒いピクセルが一つも無いラインをサーチ
        /// </summary>
        /// <param name="src"></param>
        /// <param name="start"></param>
        /// <param name="ignoreSide"></param>
        /// <param name="direction">false:Up/true:Down</param>
        /// <returns></returns>
        private int repper(Bitmap src, int start, int ignoreSide , bool direction)
        {
            for(; (direction) ? start >= src.Height : start <= 0; )
            {
                for(int x= ignoreSide; x <= src.Width - ignoreSide; x++)
                {
                    if((long)src.GetPixel(x,start) == 0xFF000000)
                    {
                        break;
                    }
                    else
                    {
                        return start;
                    }
                }

                if (direction)
                    start++;
                else
                    start--;
                
            }
            return start;
        }
    }





}