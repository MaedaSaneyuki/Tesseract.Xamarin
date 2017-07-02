using System;

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


/// <summary>
/// C:\Users\LYCEE\Documents\e612recompaile\jd-gui-windows-1.4.0\jd-gui.exe C:\Users\LYCEE\Documents\e612recompaile\base.apk_7seg\classes-dex2jar.jar 
/// </summary>

namespace Tesseract.Test.Droid
{
    [Activity (Label = "ReceiptScanner", Icon = "@drawable/icon", MainLauncher = true
        , ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation
        , ScreenOrientation = ScreenOrientation.Portrait)]
    public class TextureViewActivity : Activity, ISurfaceHolderCallback, Android.Hardware.Camera.IPreviewCallback
    {
        private bool syncObj = false;
        Android.Hardware.Camera camera;
        TesseractApi _api;
        IWindowManager windowManager;
        SurfaceView cameraSurface;
        CameraPreviewRenderer overlay; //SurfaceView sub class
        TextureView hudOverlay;

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);
            _api = new TesseractApi(this, AssetsDeployment.OncePerInitialization);
            //
            var task = _api.Init("eng");
            //task.Start();
            // task.Wait();
            //var t = task.Result;
            //_api.SetWhitelist("0123456789");
            cameraSurface = FindViewById<SurfaceView>(Resource.Id.cpPreview);
            ISurfaceHolder holder = cameraSurface.Holder;
            holder.AddCallback(this);
            holder.SetType(SurfaceType.PushBuffers);
            windowManager = this.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();

            overlay = new CameraPreviewRenderer(this, null);
            AddContentView(overlay, new ViewGroup.LayoutParams(
                                    ViewGroup.LayoutParams.MatchParent,
                                    ViewGroup.LayoutParams.FillParent));
            LinearLayout linearLayout = ConstructLayout();

            //ルートビューとして、リニアレイアウトを設定する
            AddContentView(linearLayout, new ViewGroup.LayoutParams(
                                    ViewGroup.LayoutParams.MatchParent,
                                    ViewGroup.LayoutParams.WrapContent));

        }

        private LinearLayout ConstructLayout()
        {
            //リニアレイアウトを生成
            var linearLayout = new LinearLayout(this)
            {
                Orientation = Orientation.Vertical //子コントロールを縦方向に配置する

            };

            hudOverlay = new TextureView(this);

           // WindowManager wm = (WindowManager)context.getSystemService(WINDOW_SERVICE);
            Display disp = windowManager.DefaultDisplay;

            //  hudOverlay.SetMinimumHeight(disp.Height/3);
            //hudOverlay.LayoutDirection = (1 / 3);
            linearLayout.AddView(hudOverlay,0, disp.Height*3 / 5);


            //ボタンの生成
            var button = new Button(this)
            {
                Text = "OK"
            };
            //ボタンをクリックした時のイベント処理
            button.Click += (sender, e) =>
            {
                //トーストを表示
                Toast.MakeText(this, "メッセージ", ToastLength.Short).Show();
            };

            //リニアレイアウトにボタンを追加
            linearLayout.AddView(button);
            return linearLayout;
        }

        bool apiInitializedFirst = true;


        public async void OnPreviewFrame (byte[] data, Android.Hardware.Camera camera)
        {
            //   await overlay.syncObj.WaitAsync().ConfigureAwait(false);
            if (syncObj)
                return;
            try
            {
                if (!_api.Initialized)
                    return;

                syncObj = true;
                if (apiInitializedFirst)
                {
                    _api.SetWhitelist("0123456789");
                    apiInitializedFirst = false;
                }


                var converter = new Converter();

                var cameraParameters = camera.GetParameters();
                var width = cameraParameters.PreviewSize.Width;
                var height = cameraParameters.PreviewSize.Height;
                var camfmt = cameraParameters.PreviewFormat;
                var cameraPrm = new System.Drawing.Size(width, height);
                var centerClip = new System.Drawing.Size(
                  (int)(width * 0.30)
                , (int)((height / 3) * 0.48));
                await _api.SetImage(converter.ConvertYuvToJpeg(data, camfmt, cameraPrm, centerClip));

                overlay.ToInterpret = converter.ToInterpret;

                // await _api.SetImage(ConvertYuvToJpeg(data, camera));




                //var task = _api.SetImage(ConvertYuvToJpeg(data, camera));
                //task.Wait();
                var results = _api.Results(PageIteratorLevel.Block);
                //using (var cv = hudOverlay.LockCanvas())

                overlay.ClearResults();

                foreach (var result in results)
                {
                    try
                    {
                        overlay.ImplementResult(result);

                        Log.Debug("TextureViewActivity", "Word: \"{0}\", confidence: {1}", result.Text, result.Confidence);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug("TextureViewActivity", ex.ToString());
                    }
                }
                overlay.PostInvalidate();
            //    await overlay.syncObj.WaitAsync().ConfigureAwait(false);

            }
            finally
            {
               // overlay.syncObj.Release();

                syncObj = false;
            }
            


        }

        public void SurfaceChanged (ISurfaceHolder holder, Format format, int width, int height)
        {
			
        }

        public void SurfaceCreated (ISurfaceHolder holder)
        {
            if (camera == null) {
                this.camera = Android.Hardware.Camera.Open ();
                this.camera.SetPreviewDisplay (holder);
                this.camera.SetPreviewCallback (this);


                switch (windowManager.DefaultDisplay.Rotation)
                {
                    case SurfaceOrientation.Rotation0:
                        camera.SetDisplayOrientation(90);
                        break;
                    case SurfaceOrientation.Rotation90:
                        camera.SetDisplayOrientation(0);
                        break;
                    case SurfaceOrientation.Rotation270:
                        camera.SetDisplayOrientation(180);
                        break;
                }



                this.camera.StartPreview ();
            }
        }

        public void SurfaceDestroyed (ISurfaceHolder holder)
        {
			
        }

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

            return jpegData;
        }
    }
}


//http://qiita.com/muak_x/items/c441e1e795ba22d597d6
//SleepやResumeをどう処理するか
