using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Tesseract.Test.Droid
{
    public class CameraPreviewRenderer : View
    {

        Paint p;
        public Bitmap ToInterpret { private get; set; }
        private List<Tesseract.Result> results = new List<Result>();
        
        public CameraPreviewRenderer(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
            Initialize();
        }

        public CameraPreviewRenderer(Context context, IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle)
        {
            Initialize();
        }

        private void Initialize()
        {
            SetWillNotDraw(false);

            // 塗りつぶしの設定
            p = new Paint();
            p.AntiAlias = (true);
            p.Dither=(true);
            p.Color=(Color.Black);
            p.Alpha=(128);
            p.SetStyle(Paint.Style.FillAndStroke);
        
        }

        public void ClearResults()
        {
            results.Clear();
        }

        public void ImplementResult(Tesseract.Result result)
        {
            if (result == null) return;
            var r = new Tesseract.Result();
            r.Box = result.Box;
            r.Confidence = result.Confidence;
            r.Text = result.Text;
            results.Add(r);
        }


        private int cnt = 0;
        public System.Threading.SemaphoreSlim syncObj = new System.Threading.SemaphoreSlim(1,1);

        public override void Draw(Canvas cv)
        {
            base.Draw(cv);
         //   syncObj.Wait();
            try
            {
                p.Color = Color.White;
                p.Alpha = 0;
                cv.DrawRect(0, 0, Width, Height, p);
                p.Color = Color.Black;
                p.Alpha = 128;
                cv.DrawRect(0, Height / 3, Width, Height, p);
                var src = new RectF(0, 0, Width, Height / 3);
                var centerClip = new Size(
                      (int)(src.Width() * 0.30)
                    , (int)(src.Height() * 0.48));

                var left = (src.Width() - centerClip.Width) / 2;
                var top = (src.Height() - centerClip.Height) / 2;
                var bottom = top + centerClip.Height;
                var rigtht = left + centerClip.Width;
                p.Color = Android.Graphics.Color.Wheat;
                p.SetStyle(Paint.Style.Stroke);
                p.Alpha = 255;
                p.StrokeWidth = 6;
                cv.DrawRect(left, top, rigtht, bottom, p);
                p.StrokeWidth = 1;

                p.SetStyle(Paint.Style.FillAndStroke);
                if (results.FirstOrDefault() != null && results.FirstOrDefault().Confidence > 0)
                {
                    try
                    {

                        var r = results.FirstOrDefault();
                        Android.Util.Log.Debug("overlay", "Word: \"{0}\", confidence: {1}", r.Text, r.Confidence);

                        p.Alpha = 255;
                        p.Color = Color.Blue;
                        p.TextSize = 130;

                        cv.DrawText(r.Text, r.Box.X, r.Box.Y, p);
                    }
                    catch (Exception ex)
                    {
                        Android.Util.Log.Debug("overlay", ex.ToString());
                    }
                }

                if(ToInterpret!=null&&ToInterpret.Width>0)
                    
                cv.DrawBitmap(ToInterpret, (Width- ToInterpret.Width)/2, (float)( Height * 0.4), p);


                p.Color = Color.Yellow;
                p.TextSize = 30;
                p.Alpha = 255;
                cv.DrawText((cnt++).ToString(), 0,(float)(Height * 0.9), p);
            }
            finally
            {
               // syncObj.Release();
            }
            
        }

    }
}