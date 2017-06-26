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
    [Activity (Label = "ReceiptScanner", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class TextureViewActivity : Activity, ISurfaceHolderCallback, Android.Hardware.Camera.IPreviewCallback
    {
        private bool syncObj = false;
        Android.Hardware.Camera camera;
        TesseractApi _api;
        IWindowManager windowManager;
        SurfaceView cameraSurface;
        CameraPreviewRenderer overlay;


        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);
            SetContentView (Resource.Layout.Main);
            _api = new TesseractApi (this, AssetsDeployment.OncePerInitialization);
            //_api.SetWhitelist("0123456789");
            _api.Init ("eng");
            cameraSurface = FindViewById<SurfaceView> (Resource.Id.cpPreview);
            ISurfaceHolder holder = cameraSurface.Holder;
            holder.AddCallback (this);
            holder.SetType (SurfaceType.PushBuffers);
            windowManager = this.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();

            overlay = new CameraPreviewRenderer(this, null);
            AddContentView(overlay, new ViewGroup.LayoutParams(
                                    ViewGroup.LayoutParams.MatchParent,
                                    ViewGroup.LayoutParams.FillParent));

            //リニアレイアウトを生成
            var linearLayout = new LinearLayout(this)
            {
                Orientation = Orientation.Vertical //子コントロールを縦方向に配置する
                
            };
           

            //ボタンの生成
            var button = new Button(this)
            {
                Text = "OK"
            };
            //ボタンをクリックした時のイベント処理
            button.Click += (sender, e) => {
                //トーストを表示
                Toast.MakeText(this, "メッセージ", ToastLength.Short).Show();
            };




            //リニアレイアウトにボタンを追加
            linearLayout.AddView(button);

            //ルートビューとして、リニアレイアウトを設定する
            AddContentView(linearLayout, new ViewGroup.LayoutParams(
                                    ViewGroup.LayoutParams.MatchParent,
                                    ViewGroup.LayoutParams.WrapContent));

        }
        


        public async void OnPreviewFrame (byte[] data, Android.Hardware.Camera camera)
        {
            if (syncObj)
                return;
            if (!_api.Initialized)
                return;
            syncObj = true;
            await _api.SetImage(ConvertYuvToJpeg(data, camera));
            //var task = _api.SetImage(ConvertYuvToJpeg(data, camera));
            //task.Wait();
            var results = _api.Results (PageIteratorLevel.Block);

            foreach (var result in results) {


                Log.Debug ("TextureViewActivity", "Word: \"{0}\", confidence: {1}", result.Text, result.Confidence);
            }
            syncObj = false;
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
