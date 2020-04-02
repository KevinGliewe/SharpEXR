using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace SharpEXR.Viewer.HTML
{
    class Program
    {
        static void Main()
        {
            var args = CommandLineArguments.Parse();

            var img = EXRFile.FromFile(args[0]);
            var part = img.Parts[0];
            //part.OpenParallel(() => { return new EXRReader(new System.IO.BinaryReader(System.IO.File.OpenRead(args[0]))); });
            part.OpenParallel(args[0]);

            var bmp = new Bitmap(part.DataWindow.Width, part.DataWindow.Height);
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            var destBytes = part.GetBytes(ImageDestFormat.BGRA8, GammaEncoding.sRGB, data.Stride);
            Marshal.Copy(destBytes, 0, data.Scan0, destBytes.Length);
            bmp.UnlockBits(data);

            part.Close();

            var ms = new MemoryStream();

            bmp.Save(ms, ImageFormat.Png);
            var buffer = new byte[ms.Position];
            ms.Position = 0;
            ms.Read(buffer, 0, buffer.Length);
            ms.Dispose();

            var image64 = System.Convert.ToBase64String(buffer);

            var html = $@"
                <html>
                    <header><title>{args[0]}</title></header>
                    <body>
                        <img src=""data: image / png; base64,{image64}""
                    </body>
                </html>
            ";

            var htmlFile = Path.GetTempFileName() + ".html";
            File.WriteAllText(htmlFile, html);

            var openCommand = "";

            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    openCommand = "start";
                    break;
                case PlatformID.MacOSX:
                    openCommand = "open";
                    break;
                case PlatformID.Unix:
                    openCommand = "xdg-open";
                    break;
            }

            Console.WriteLine($"{openCommand} {htmlFile}".Sh());

        }
    }
}
