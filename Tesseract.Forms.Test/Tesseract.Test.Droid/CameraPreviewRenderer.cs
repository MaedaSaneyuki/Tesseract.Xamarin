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

        Paint paint;





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
  
            // 塗りつぶしの設定
            paint = new Paint();
            paint.AntiAlias = (true);
            paint.Dither=(true);
            paint.Color=(Color.Magenta);
            paint.Alpha=(128);
            paint.SetStyle(Paint.Style.FillAndStroke);
        
        }

        public override void Draw(Canvas canvas)
        {
            base.Draw(canvas);

            var center = Width / 2;
            var r = (float)(center * 0.95);


            canvas.DrawCircle(center, center, r, paint);

            // 短形で塗りつぶす
            //canvas.DrawRect(33,.rect, mPaint);

        }

    }
}