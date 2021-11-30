using IronOcr;
using JapaneseOCR.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Drawing.Point;

namespace JapaneseOCR
{

   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window, INotifyPropertyChanged
   {
      public event PropertyChangedEventHandler PropertyChanged;
      private Point _startSelection;
      private Point _endSelection;
      private bool _isClipping;
      private List<Window> openWindows;

      private string _ocrText;
      public string OCRText
      {
         get
         {
            return _ocrText;
         }
         set
         {
            if (value != _ocrText)
            {
               _ocrText = value;
               OnPropertyChanged("OCRText");
            }
         }
      }

      public MainWindow()
      {
         InitializeComponent();
         this.DataContext = this;
         this.openWindows = new List<Window>();
      }

      private void ClipAndTranslate()
      {
         // close the overlay so as not to effect the colors of the pixels that we are clipping
         DisableClip();
         
         // clip rectangle from screen pixels, currently must be a rectangle starting in upper left to bottom right
         // TODO: add logic to clip from bottom right to top left
         int xdiff = (int)Math.Abs(this._endSelection.X - this._startSelection.X);
         int ydiff = (int)Math.Abs(this._endSelection.Y - this._startSelection.Y);
         System.Drawing.Rectangle bounds = new System.Drawing.Rectangle((int)this._startSelection.X, (int)this._startSelection.Y, xdiff, ydiff);
         System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(xdiff, ydiff);
         using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bitmap))
         {
            g.CopyFromScreen((int)this._startSelection.X, (int)this._startSelection.Y, 0, 0, bounds.Size);
         }
         BitmapData bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
         BitmapSource source = Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
         System.Windows.Clipboard.SetImage(source);
         bitmap.UnlockBits(bitmapData);
         this.clipboardImage.Source = source;
         
         // parse image to japanese text
         IronTesseract ocr = new IronTesseract();
         ocr.Language = OcrLanguage.Japanese;
         ocr.Configuration.TesseractVariables["preserve_interword_spaces"] = true;
         OcrResult result = ocr.Read(bitmap);

         // strips whitespace and new lines
         // japanese doesnt need whitespaces and the ocr will insert blank lines based on space from the image
         string resultText = Regex.Replace(result.Text, @"\s+", string.Empty);
         this.OCRText = resultText;
         Uri uri = new Uri($"https://jisho.org/search/{resultText}", UriKind.Absolute);
         this.browser.Navigate(uri);
      }

      protected void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }



      protected override void OnClosing(CancelEventArgs e)
      {
         NativeMethods.ClearHook();
         base.OnClosing(e);
      }

      private void startClipButton_Click(object sender, RoutedEventArgs e)
      {
         foreach(Screen screen in Screen.AllScreens)
         {
            Window window = new Window();
            window.Left = screen.WorkingArea.Left;
            window.Top = screen.WorkingArea.Top;
            window.Width = screen.WorkingArea.Width;
            window.Height = screen.WorkingArea.Height;
            window.Topmost = true;
            window.AllowsTransparency = true;
            window.WindowStyle = WindowStyle.None;
            window.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 255, 255, 255));
            Canvas canvas = new Canvas();
            canvas.Width = window.Width;
            canvas.Height = window.Height;
            window.Content = canvas;
            openWindows.Add(window);
            window.Show();
         }

         NativeMethods.MouseHandle = NativeMouseHandle;
         NativeMethods.KeyboardHandle = NativeKeyboardHandle;
      }

      public void NativeMouseHandle(NativeMethods.MouseEventModel info)
      {
         switch (info.EventType)
         {
            case NativeMethods.MouseMessage.WM_LBUTTONDOWN:
               this._startSelection = info.Coordinates;
               this._isClipping = true;
               break;
            case NativeMethods.MouseMessage.WM_LBUTTONUP:
               if (this._isClipping)
               {
                  this._endSelection = info.Coordinates;
                  if (Math.Abs(this._endSelection.X - this._startSelection.X) > 10 && Math.Abs(this._endSelection.Y - this._startSelection.Y) > 10)
                  {
                     this.ClipAndTranslate();
                  }
               }
               this._isClipping = false;
               break;
            case NativeMethods.MouseMessage.WM_MOUSEMOVE:
               // draw highlight rect
               break;
         }
      }

      public void NativeKeyboardHandle(NativeMethods.KeyboardEventModel info)
      {
         if(info.EventType == NativeMethods.KeyboardMessage.WM_KEYDOWN && info.Key == Keys.Escape)
         {
            DisableClip();
         }
      }

      private void DisableClip()
      {
         foreach (Window window in openWindows)
         {
            window.Close();
         }
         openWindows.Clear();
         NativeMethods.MouseHandle = null;
         NativeMethods.KeyboardHandle = null;
         this._isClipping = false;
      }
   }
}
