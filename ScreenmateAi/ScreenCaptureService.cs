using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace ChatGPTWPF.Services
{
    public class ScreenCaptureService
    {
       
        public string CaptureScreen()
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;

            using Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);

            using Graphics graphics = Graphics.FromImage(bitmap);

            graphics.CopyFromScreen(
                bounds.X,
                bounds.Y,
                0,
                0,
                bounds.Size
            );

            string path = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";

            bitmap.Save(path, ImageFormat.Png);

            return path;
        }

        
    }
}
