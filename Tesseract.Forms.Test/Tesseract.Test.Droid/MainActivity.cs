using System;
using System.Collections.Generic;
using System.Linq;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Graphics;
using System.IO;
using Tesseract.Droid;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Android.Content.PM;
using Java.IO;
using Android.Util;

namespace Tesseract.Test.Droid
{
    [Activity (Label = "ReceiptScanner", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class TextureViewActivity : Activity, ISurfaceHolderCallback, Android.Hardware.Camera.IPreviewCallback
    {
        private const System.String TAG = "TextureViewActivity";
        private bool syncObj = false;
        Android.Hardware.Camera camera;
        TesseractApi _api;
        private bool debugOnce = false;
        private CameraOverlayView overlay;
        private ConvertYuvToJpeg converter;
        //        private int convertYLevel = 220;
        private int convertYLevel = 94;
        private int prevGetY;

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);
            SetContentView (Resource.Layout.Main);
            _api = new TesseractApi (this, AssetsDeployment.OncePerVersion);
            _api.Init ("eng");
            _api.SetWhitelist("0123456789");
            SurfaceView cameraSurface = FindViewById<SurfaceView> (Resource.Id.cpPreview);
            ISurfaceHolder holder = cameraSurface.Holder;
            holder.AddCallback (this);
            holder.SetType (SurfaceType.PushBuffers);

            overlay = new CameraOverlayView(this);
            AddContentView(overlay,  new ViewGroup.LayoutParams(ViewGroup.LayoutParams.FillParent,ViewGroup.LayoutParams.FillParent));

            converter = new ConvertYInvertToJpeg(this.SaveBitmap,convertYLevel);

            cameraSurface.Touch += (s, e) =>
            {

                System.Diagnostics.Debug.WriteLine("cameraSurface.Touch e.Event.Action=" + e.Event.Action.ToString());
            };

            overlay.Touch += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine("overlay.Touch e.Event.Action=" + e.Event.Action.ToString());
                if(e.Event.Action == MotionEventActions.Down)
                {
                    camera.StopPreview();
                }
                else if (e.Event.Action == MotionEventActions.Move)
                {
                    if(Math.Abs( e.Event.GetY() - prevGetY) > 3 )
                    {
                        if(e.Event.GetY() - prevGetY > 0)
                        {
                            convertYLevel = Math.Min(0xff, convertYLevel + 1);
                        }
                        else
                        {
                            convertYLevel = Math.Max(0x0, convertYLevel - 1);
                        }
                    }
                    prevGetY = (int)e.Event.GetY();

                }
                else if (e.Event.Action == MotionEventActions.Up)
                {
                    //if(converter.GetType() == typeof(ConvertYuvToJpeg))
                    if (true)
                    {
                        //converter = new ConvertYToJpeg(this.SaveBitmap, convertYLevel);
                        converter = new ConvertYInvertToJpeg(this.SaveBitmap, convertYLevel);
                    }
                    else if(converter.GetType() == typeof(ConvertYToJpeg))
                    {
                        converter = new ConvertYuvToJpeg(this.SaveBitmap);
                    }
                    camera.StartPreview();
                }

            };


        }

        public async void OnPreviewFrame (byte[] data, Android.Hardware.Camera camera)
        {
            if (syncObj)
                return;
            if (!_api.Initialized)
                return;
            syncObj = true;
            var preproData = converter.Convert(data, camera);
            overlay.PreprocessingPreviewFrame = BitmapFactory.DecodeByteArray(preproData, 0, preproData.Length, new BitmapFactory.Options { InSampleSize = 1 }); 

            await _api.SetImage (preproData);
            var results = _api.Results (PageIteratorLevel.Word);
            //foreach (var result in results) {
            //    Log.Debug ("TextureViewActivity", "Word: \"{0}\", confidence: {1}", result.Text, result.Confidence);
            //}
            try
            {
                if (results.Count() == 0)
                {
                    overlay.Results = null;
                }
                else
                {
                    overlay.Results = new List<Result>( results);
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
            
            syncObj = false;
        }

        public void SurfaceChanged (ISurfaceHolder holder, Format format, int width, int height)
        {
			
        }

        public void SurfaceCreated (ISurfaceHolder holder)
        {
            if (camera == null) {
                camera = Android.Hardware.Camera.Open ();
                camera.SetDisplayOrientation(90);
                camera.SetPreviewDisplay (holder);
                camera.SetPreviewCallback (this);
                var param = camera.GetParameters();
                var p = from k in camera.GetParameters().SupportedPreviewSizes orderby k.Width descending select k ;
                foreach (var l in  p )
                {
                    Log.WriteLine(LogPriority.Info, "", string.Format("l.Width={0}", l.Width));
                    if (l.Width <=1240)
                    {
                        param.SetPreviewSize(l.Width, l.Height);
                        break;
                    }
                }
                                
                //p.PreviewSize = new Android.Hardware.Camera.Size(Android.Hardware.Camera, 500, 500);
                //p.PreviewFormat = Android.Graphics.ImageFormatType.Nv21;
                camera.SetParameters(param);
                camera.StartPreview ();

                var cameraParameters = camera.GetParameters();
                var width = cameraParameters.PreviewSize.Width;
                var height = cameraParameters.PreviewSize.Height;
                overlay.previewSize = new Rectangle(0, 0, height, width / 3);

            }
        }

        public void SurfaceDestroyed (ISurfaceHolder holder)
        {
			
        }

        private bool debugSwitch = false;

        private byte[] ConvertYuvToJpeg (byte[] yuvData, Android.Hardware.Camera camera)
        {
            var cameraParameters = camera.GetParameters ();
            var width = cameraParameters.PreviewSize.Width;
            var height = cameraParameters.PreviewSize.Height;
            var yuv = new YuvImage (yuvData, cameraParameters.PreviewFormat, width, height, null);   
            var ms = new MemoryStream ();
            var quality = 80;   // adjust this as needed
            yuv.CompressToJpeg (new Rect (0, 0, width, height), quality, ms);
            var jpegData = ms.ToArray ();
            //byte[] jpegData = null;

            try
            {
                // rotate
                //var rotatedMs = new MemoryStream();

                var src = BitmapFactory.DecodeByteArray(jpegData, 0, jpegData.Length, new BitmapFactory.Options { InSampleSize = 1 });
                //var src = BitmapFactory.DecodeStream(ms);  // todo 何故かnullを返す
                var rotate = Bitmap.CreateBitmap(src.Height, src.Width / 3, src.GetConfig());
                var cv = new Canvas(rotate);
                Paint p = new Paint();
                p.AntiAlias = false;
                p.SetStyle(Paint.Style.Fill);

                //if (debugSwitch) SaveBitmap(src);

                cv.Save(SaveFlags.All);

                var delata = Math.Abs(src.Height- src.Width);
                cv.Rotate(90,src.Width/2  , src.Height/2 );
                cv.DrawBitmap(src, delata/2, delata/2, p);
                cv.Restore();

                p.Color = Color.White;
                var r = (int)(src.Height * 0.1);
                var r2 = (int)(src.Height * 0.3);
                cv.DrawRect(0, 0, r2, rotate.Height,p);
                cv.DrawRect(0, 0, rotate.Width, r, p);
                cv.DrawRect(rotate.Width - r2, 0, rotate.Width, rotate.Height, p);
                cv.DrawRect(0, rotate.Height - r, rotate.Width, rotate.Height, p);

                //return cv.ToArray<byte>();
                if (!debugSwitch) SaveBitmap(rotate);
                overlay.PreprocessingPreviewFrame = rotate;
                //cv.Restore();

                var ms2 = new MemoryStream();
                rotate.Compress(Bitmap.CompressFormat.Jpeg, quality, ms2);
                //SaveBitmap(ms2);

                return ms2.ToArray() ;

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return jpegData;
            }
        }

        private void SaveBitmap(Bitmap bitmap )
        {
            if (!debugOnce) return;
            var path = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures).Path;
            var observePath = System.IO.Path.Combine(path, "Observe");
            var pngfileName = System.IO.Path.Combine(observePath, $"{DateTime.Now.ToString("MMddHHmmssfff")}.jpg");
            var di = new DirectoryInfo(observePath);
            Log.Debug(TAG, pngfileName);
            if (!di.Exists) di.Create();

            using (var fs = new System.IO.FileStream(pngfileName, System.IO.FileMode.OpenOrCreate))
            {
                fs.SetLength(0);
                bitmap.Compress(Bitmap.CompressFormat.Jpeg, 80, fs);
                fs.Close();
            }
            debugOnce = false;
        }

        private void SaveBitmap(Stream stream)
        {
            if (!debugOnce) return;
            var path = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures).Path;
            var observePath = System.IO.Path.Combine(path, "Observe");
            var pngfileName = System.IO.Path.Combine(observePath, $"{DateTime.Now.ToString("MMddHHmmssfff")}.jpg");
            var di = new DirectoryInfo(observePath);
            Log.Debug(TAG, pngfileName);
            if (!di.Exists) di.Create();

            using (var fs = new System.IO.FileStream(pngfileName, System.IO.FileMode.OpenOrCreate))
            {
                fs.SetLength(0);
                //bitmap.Compress(Bitmap.CompressFormat.Jpeg, 99, fs);
                stream.CopyTo(fs);
                fs.Close();
            }
            debugOnce = false;
        }


    }
}


