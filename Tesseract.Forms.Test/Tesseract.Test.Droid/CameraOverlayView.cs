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
using Tesseract;

namespace Tesseract.Test.Droid
{
    public class CameraOverlayView : View
    {
        private IEnumerable<Result> _Results;
        private Bitmap _PreprocessingPreviewFrame;

        public IEnumerable<Result> Results {
            get
            { return _Results; }
            set
            {
                _Results = value;
                this.Invalidate();
            }
        }
        public Rectangle previewSize { get; set; }
        public Bitmap PreprocessingPreviewFrame {
            get
            { return _PreprocessingPreviewFrame; }
            set
            {
                _PreprocessingPreviewFrame = value;
                this.Invalidate();
            }
        }

        public CameraOverlayView(Context context) :base(context)
        {
            this.SetBackgroundColor(Color.Transparent);
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            using (var paint = new Paint())
            {
                //  TestDraw1(canvas, paint);
                oneThirdBox(canvas, paint);
                if(Results!=null) resultsBoxes(canvas, paint);
                if (PreprocessingPreviewFrame != null) preprocessingPreviewFrameBox(canvas, paint);
            }

        }

        private void preprocessingPreviewFrameBox(Canvas canvas, Paint paint)
        {
            paint.AntiAlias = false;

            var r = (int)(Width * 0.05);
            var oneThirdBottomCenter = new Rect(
                (Width-PreprocessingPreviewFrame.Width)/2 , 
                Height / 3 + r + PreprocessingPreviewFrame.Height /2, 
                PreprocessingPreviewFrame.Width, 
                PreprocessingPreviewFrame.Height);
            canvas.DrawBitmap(PreprocessingPreviewFrame, oneThirdBottomCenter.Left, oneThirdBottomCenter.Top,null);

        }

        private Rect ReduceBox(Rectangle rectangle)
        {
            return new Rect(
                (int)(rectangle.Left * Width / previewSize.Width),
                (int)(rectangle.Top * (Height / 3) / previewSize.Height),
                (int)(rectangle.Width * Width / previewSize.Width),
                (int)(rectangle.Height * (Height / 3) / previewSize.Height)
                );
        }

        private void resultsBoxes(Canvas canvas, Paint paint)
        {

            paint.AntiAlias = true;

            var rLeft = (int)(Width * 0.05);

            paint.SetStyle(Paint.Style.FillAndStroke);
            paint.StrokeWidth = 1;
            paint.Color = Color.Red;
            paint.Alpha = 0xff;
            paint.TextSize = paint.TextSize * 10;

            try
            {
                //if (Results.Count() == 0)
                //{
                //    System.Diagnostics.Debug.WriteLine("if (Results.Count() == 0 )");
                //    return;
                //}

                //foreach (var r in Results)
                //foreach (var r in Results)
                var r = Results.First();
                {
                    if (string.IsNullOrEmpty(r.Text)) return;
                    if (r.Confidence > 0.8)
                        paint.Color = Color.Blue;
                    else
                        paint.Color = Color.Red;
                    var rBox = ReduceBox(r.Box);
                    paint.SetStyle(Paint.Style.Stroke);
                    canvas.DrawRect(rBox, paint);
                    paint.SetStyle(Paint.Style.FillAndStroke);
                    var left = (int)Math.Max(rLeft, rBox.Left);
                    canvas.DrawText(r.Text, left, rBox.Top + rBox.Height() + paint.TextSize, paint);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine( ex.ToString());
            }

        }

        private void oneThirdBox(Canvas canvas, Paint paint)
        {
            paint.AntiAlias = true;

            var center = Width / 2;
            var r = (int)(Width * 0.1);
            var r2 = (int)(Width * 0.3);
            var r3 = (int)(Height/3 * 0.3);
            var oneThird = new Rect(r2, r, Width - r2, Height / 3 - r);

            paint.SetStyle(Paint.Style.Fill);
            paint.Color = Color.Black;
            paint.Alpha = 0x80;
            canvas.DrawRect(new Rect(0, Height / 3, Width,Height) , paint);

            paint.SetStyle(Paint.Style.Stroke);
            paint.StrokeWidth = r/2;
            paint.Color = new Color(0x49, 0x71, 0xFF);
            paint.Alpha = 0xC0;
            //if (drucking) paint.Color = Color.Pink;
            canvas.DrawRect(oneThird, paint);


            paint.Color = Color.Red;
            paint.StrokeWidth = 1;
            paint.Alpha = 0xff;
            canvas.DrawLine(r, r3, Width - r, r3, paint);
            canvas.DrawLine(r, Height / 3 - r3, Width - r, Height / 3 - r3, paint);

        }

    }
}