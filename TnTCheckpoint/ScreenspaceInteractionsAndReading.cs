using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;
using System.Windows.Media;
using NetCord;
using NetCord.Gateway;
using Tesseract;
using static TnTCheckpoint.ConstantsAndGlobals;
using static TnTCheckpoint.DebugCommunication;
using static TnTCheckpoint.DLLImportsStructsAndEnums;
using Color = System.Drawing.Color;

namespace TnTCheckpoint
{
    public class ScreenspaceInteractionsAndReading
    {

        public static async void AwaitColorChange(double percentageposx, double percentageposy, int count)
        {
            long starttime = DateTime.Now.AddSeconds(60).Ticks;
            int colorchangecount = 0;
            Point TempLocation = ConvertAspectRatioCoords(percentageposx, percentageposy);
            Color TempColor = GetColorAt(TempLocation);
            Color col = TempColor;
            bool worked = true;

            visualupdates = colorchangecount;
            visualupdatestotal = count;
            visualupdatesx = TempLocation.X;
            visualupdatesy = TempLocation.Y;
            runningupdatedetection = true;
            UpdateTextDisplay();

            TempColor = GetColorAt(TempLocation);

            while (colorchangecount < count)
            {
                if (TempColor != col)
                {
                    //get average so that steady shifts dont count.
                    double avg = (TempColor.R + TempColor.G + TempColor.B) / 3;
                    double oldavg = (col.R + col.G + col.B) / 3;
                    if (Math.Abs(oldavg - avg) > 9)
                    {
                        col = TempColor;
                        colorchangecount++;
                        starttime = DateTime.Now.AddSeconds(60).Ticks;
                        System.Windows.Media.Color col2 = Colors.White;
                        col2.R = TempColor.R;
                        col2.G = TempColor.G;
                        col2.B = TempColor.B;

                        visualupdates = colorchangecount;
                        visualupdatestotal = count;
                        visualupdatesx = TempLocation.X;
                        visualupdatesy = TempLocation.Y;
                        UpdateTextDisplay();
                    }
                    else
                    {
                        if (DateTime.Now.Ticks > starttime)
                        {
                            worked = false;
                            break;
                        }
                    }
                }
                TempColor = GetColorAt(TempLocation);
                Task.Delay(33).Wait();
            }

            runningupdatedetection = false;
            UpdateTextDisplay();
            if (!worked)
            {
                return;
            }
        }

        public static async void SendClick(Point point)
        {
            uint gox = (uint)point.X;
            uint goy = (uint)point.Y;
            mouse_event((uint)MouseEvents.MOUSEEVENTF_LEFTDOWN, gox, goy, 0, 0);
            Task.Delay(101).Wait();
            mouse_event((uint)MouseEvents.MOUSEEVENTF_LEFTUP, gox, goy, 0, 0);
        }

        public static Color GetColorAt(Point coordinates)
        {
            Bitmap ColorCheckBitmap = new Bitmap(1, 1);

            ColorCheckBitmap = new Bitmap(1, 1);
            Rectangle bounds = new Rectangle(coordinates.X, coordinates.Y, 1, 1);
            Graphics g = Graphics.FromImage(ColorCheckBitmap);
            Size s = bounds.Size;
            g.CopyFromScreen(bounds.Location, Point.Empty, s);

            Color col = ColorCheckBitmap.GetPixel(0, 0);

            ColorCheckBitmap.Dispose();
            g.Dispose();

            return col;
        }

        public static Point ConvertAspectRatioCoords(double xper, double yper)
        {
            //convert to 16x9 for u.i. with a border for all math.
            int width = d2window.Right - d2window.Left;
            int height = d2window.Bottom - d2window.Top;
            double ratio = width * 1.0 / height;
            double desiredratio = 16.0 / 9.0;
            int leftbuffer = 0;
            int topbuffer = 0;

            if (ratio != desiredratio)
            {
                if (ratio > desiredratio)
                {
                    //width is too big
                    int temp = (int)(height / 9 * 16);
                    leftbuffer = (width - temp) / 2;
                    width = temp;
                }
                else
                {
                    //height is too big
                    int temp = (int)(width / 16 * 9);
                    leftbuffer = (height - temp) / 2;
                    height = temp;
                }
            }

            int pointx = (int)Math.Round(leftbuffer + (width * (xper / 100)));
            int pointy = (int)Math.Round(topbuffer + (height * (yper / 100)));

            return new Point(pointx, pointy);
        }

        public static string GetText(Bitmap imgsource)
        {
            var ocrtext = string.Empty;
            using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
            {
                engine.SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ':()");
                using (var img = PixConverter.ToPix(imgsource))
                {
                    using (var page = engine.Process(img))
                    {
                        ocrtext = page.GetText();
                    }
                }
            }

            imgsource.Dispose();

            return ocrtext;
        }

        public static void AwaitText(string inputtext, Point coordinatepointstart, Point coordinatepointend)
        {
            string currenttext = "";
            bool match = false;
            while (StringDifference(inputtext, currenttext) < .9)
            {
                int width = d2window.Right - d2window.Left;
                int height = d2window.Bottom - d2window.Top;
                int iconwidth = coordinatepointend.X - coordinatepointstart.X;
                int iconheight = coordinatepointend.Y - coordinatepointstart.Y;
                int xpos = coordinatepointstart.X;
                int ypos = coordinatepointstart.Y;

                Bitmap bmpScreenshot = new Bitmap(iconwidth, iconheight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Graphics g = Graphics.FromImage(bmpScreenshot);

                g.CopyFromScreen(xpos, ypos, 0, 0, new System.Drawing.Size(iconwidth, iconheight));

                bmpScreenshot.ApplyEffect(new System.Drawing.Imaging.Effects.GrayScaleEffect());
                bmpScreenshot.ApplyEffect(new System.Drawing.Imaging.Effects.BrightnessContrastEffect(-25, 0));
                bmpScreenshot.ApplyEffect(new System.Drawing.Imaging.Effects.BrightnessContrastEffect(0, 100));
                bmpScreenshot.ApplyEffect(new System.Drawing.Imaging.Effects.BlurEffect(2, false));
                bmpScreenshot.ApplyEffect(new System.Drawing.Imaging.Effects.InvertEffect());;

                string ocrstring = GetText(bmpScreenshot).ToLower().Replace("(", "").Replace(")", "").Replace("'", "").Replace(":", "").Replace("\n", "");
                bmpScreenshot.Dispose();

                Task.Delay(33).Wait();
                currenttext = ocrstring;
            }
        }

        public static double StringDifference(string s1, string s2)
        {
            char[] c1 = s1.ToLower().ToArray();
            char[] c2 = s2.ToLower().ToArray();

            int length = c1.Count();
            if (c2.Count() < length) length = c2.Count();

            if (length == 0) return 0;

            double same = 0;
            for (int i = 0; i < length; i++)
            {
                if (c1[i] == c2[i]) same++;
            }

            double match = same / length;

            return match;
        }

        public static bool CheckBlackScreen()
        {
            Color col1 = GetColorAt(ConvertAspectRatioCoords(25, 75));
            int avg1 = (col1.R + col1.G + col1.B) / 3;
            Color col2 = GetColorAt(ConvertAspectRatioCoords(75, 25));
            int avg2 = (col2.R + col2.G + col2.B) / 3;
            Color col3 = GetColorAt(ConvertAspectRatioCoords(50, 50));
            int avg3 = (col3.R + col3.G + col3.B) / 3;

            //3 point check for clarity sake
            if (avg1 < 10 &
                avg2 < 10 &
                avg3 < 10)
            {
                //its dark
                if (avg1 == avg2 & avg2 == avg3) return true; //its all the same shade as well
            }
            return false;
        }

        public static bool CheckCharSelect()
        {
            //make sure im in mnk mode
            mouse_event((uint)MouseEvents.MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
            Task.Delay(101).Wait();
            mouse_event((uint)MouseEvents.MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
            Task.Delay(101).Wait();

            int width = d2window.Right - d2window.Left;
            int height = d2window.Bottom - d2window.Top;

            Point startcoords = ConvertAspectRatioCoords(0.5, 95.972222222);
            Point endcoords = ConvertAspectRatioCoords(14.0625, 98.75);
            int iconwidth = (int)endcoords.X - startcoords.X;
            int iconheight = (int)endcoords.Y - startcoords.Y;

            int xpos = startcoords.X;
            int ypos = startcoords.Y;

            statussubtext = "Return to orbit: Double checking I'm not already on character select.";
            UpdateTextDisplay();

            bool works = false;
            string ocrstring = "";
            bool select = false;

            while (!works)
            {
                try
                {
                    Bitmap bmpScreenshot = new Bitmap(iconwidth, iconheight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    Graphics g = Graphics.FromImage(bmpScreenshot);
                    Thread.Sleep(500);
                    g.CopyFromScreen(xpos, ypos, 0, 0, new System.Drawing.Size(iconwidth, iconheight));
                    Thread.Sleep(200);
                    bmpScreenshot.ApplyEffect(new System.Drawing.Imaging.Effects.GrayScaleEffect());
                    bmpScreenshot.ApplyEffect(new System.Drawing.Imaging.Effects.BrightnessContrastEffect(-50, 0));
                    bmpScreenshot.ApplyEffect(new System.Drawing.Imaging.Effects.BrightnessContrastEffect(0, 100));
                    bmpScreenshot.ApplyEffect(new System.Drawing.Imaging.Effects.BlurEffect(2, false));
                    bmpScreenshot.ApplyEffect(new System.Drawing.Imaging.Effects.InvertEffect());
                    Thread.Sleep(500);

                    ocrstring = GetText(bmpScreenshot).ToLower().Replace("(", "").Replace(")", "").Replace("'", "").Replace(":", "").Replace("\n", "");
                    bmpScreenshot.Dispose();
                    works = true;
                }
                catch
                {

                }
            }

            if (ocrstring.Contains("exittodesktop"))
            {
                statussubtext = "Return to orbit: Turns out I'm already there. Going to idle...";
                UpdateTextDisplay();
                flagOnCharSelect = true;
                select = true;
            }

            return select;
        }

        public static string CheckIfOrbitTextBox()
        {
            int width = d2window.Right - d2window.Left;
            int height = d2window.Bottom - d2window.Top;
            int iconwidth = (int)Math.Round(width * 0.073125);
            int iconheight = (int)Math.Round(height * 0.04362);
            Point startcoords = ConvertAspectRatioCoords(34.3125, 49.625);
            int xpos = startcoords.X;
            int ypos = startcoords.Y;

            Bitmap bmpScreenshot = new Bitmap(iconwidth, iconheight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bmpScreenshot);
            g.CopyFromScreen(xpos, ypos, 0, 0, new System.Drawing.Size(iconwidth, iconheight));
            bmpScreenshot.ApplyEffect(new System.Drawing.Imaging.Effects.GrayScaleEffect());
            bmpScreenshot.ApplyEffect(new System.Drawing.Imaging.Effects.BrightnessContrastEffect(-25, 0));
            bmpScreenshot.ApplyEffect(new System.Drawing.Imaging.Effects.BrightnessContrastEffect(0, 100));
            bmpScreenshot.ApplyEffect(new System.Drawing.Imaging.Effects.BlurEffect(2, false));
            bmpScreenshot.ApplyEffect(new System.Drawing.Imaging.Effects.InvertEffect());

            Task.Delay(1000).Wait();

            string ocrstring = GetText(bmpScreenshot).ToLower().Replace("(", "").Replace(")", "").Replace("'", "").Replace(":", "").Replace("\n", "");
            bmpScreenshot.Dispose();

            return ocrstring;
        }

        public static bool WaitForPlayerJoinOrbit(DateTime timeout)
        {
            Point pointcheck = ConvertAspectRatioCoords(95.859, 83.75);
            Point pointclick = ConvertAspectRatioCoords(75.117, 83.75);
            SetCursorPos(pointclick.X, pointclick.Y);
            Task.Delay(1000).Wait();
            Color spotcolor = GetColorAt(pointcheck);
            bool change = false;

            statussubtext = "Comparing red values to see launch button go red.";
            UpdateTextDisplay();

            while (!change)
            {
                Color spotcolor2 = GetColorAt(pointcheck);

                if (Math.Abs(spotcolor.G - spotcolor2.G) > 60)
                {
                    if (spotcolor2.G != 0) change = true;
                }

                if (DateTime.Now > timeout)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Nobody joined. Returning to orbit.");

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return false;
                }
                Task.Delay(101).Wait();
            }

            AwaitColorChange(95.859, 83.75, 1); //wait for it to stop being red.

            return true;
        }

        public static void WaitThruBlackscreens(string activity)
        {
            statussubtext = "Awaiting first black screen.";
            UpdateTextDisplay();

            Task.Delay(3000).Wait();

            DateTime bailout = DateTime.Now.AddSeconds(30);

            while (!CheckBlackScreen())
            {
                if (DateTime.Now > bailout) break;
                Task.Delay(30).Wait();
            }
            while (CheckBlackScreen())
            {
                Task.Delay(30).Wait();
            }
            AwaitColorChange(3, 3, 1); //come out of black screen

            bailout = DateTime.Now.AddSeconds(65);

            if (activity != "RON" & activity != "PR")
            {

                statussubtext = "Waiting for second black screen...";
                UpdateTextDisplay();

                while (!CheckBlackScreen()) //wait until on next black screen
                {
                    if (DateTime.Now > bailout) break;
                    Task.Delay(30).Wait();
                }

                while (CheckBlackScreen())//black screen found, wait for it to go away
                {
                    Task.Delay(30).Wait();
                }

                if (DateTime.Now < bailout) AwaitColorChange(90, 90, 1); //come out of black screen, boots on ground
            }
            Task.Delay(3000).Wait();
        }
    }
}
