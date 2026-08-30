using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using NetCord;
using NetCord.Gateway;
using NetCord.Logging;
using WindowsInput;
using WindowsInput.Native;
using static TnTCheckpoint.ConstantsAndGlobals;
using static TnTCheckpoint.DLLImportsStructsAndEnums;
using static TnTCheckpoint.DebugCommunication;
using static TnTCheckpoint.ScreenspaceInteractionsAndReading;
using static TnTCheckpoint.CommandHandling;
using static TnTCheckpoint.Macros;

namespace TnTCheckpoint
{
    public class StartupAndInitialization
    {

        public static void InitializeCheckpoints()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            Checkpoints["CE"] = new Dictionary<string, int>();
            Checkpoints["DSC"] = new Dictionary<string, int>();
            Checkpoints["DPE"] = new Dictionary<string, int>();
            Checkpoints["DP"] = new Dictionary<string, int>();
            Checkpoints["SE"] = new Dictionary<string, int>();
            Checkpoints["RON"] = new Dictionary<string, int>();
            Checkpoints["KF"] = new Dictionary<string, int>();
            Checkpoints["VOW"] = new Dictionary<string, int>();
            Checkpoints["VOG"] = new Dictionary<string, int>();
            Checkpoints["GOS"] = new Dictionary<string, int>();
            Checkpoints["LW"] = new Dictionary<string, int>();
            Checkpoints["WR"] = new Dictionary<string, int>();
            Checkpoints["PIT"] = new Dictionary<string, int>();
            Checkpoints["EQ"] = new Dictionary<string, int>();
            Checkpoints["SD"] = new Dictionary<string, int>();
            Checkpoints["VH"] = new Dictionary<string, int>();
            Checkpoints["GOTD"] = new Dictionary<string, int>();
            Checkpoints["SOTW"] = new Dictionary<string, int>();
            Checkpoints["D"] = new Dictionary<string, int>();
            Checkpoints["GOA"] = new Dictionary<string, int>();
            Checkpoints["PR"] = new Dictionary<string, int>();
            Checkpoints["ST"] = new Dictionary<string, int>();
            Checkpoints["CR"] = new Dictionary<string, int>();
            Checkpoints["MS"] = new Dictionary<string, int>();
            Checkpoints["GAUNTLET"] = new Dictionary<string, int>();

            Checkpoints["masterSE"] = new Dictionary<string, int>();
            Checkpoints["masterVOG"] = new Dictionary<string, int>();
            Checkpoints["masterCE"] = new Dictionary<string, int>();
            Checkpoints["masterRON"] = new Dictionary<string, int>();
            Checkpoints["masterKF"] = new Dictionary<string, int>();
            Checkpoints["masterVOW"] = new Dictionary<string, int>();
            Checkpoints["masterVH"] = new Dictionary<string, int>();
            Checkpoints["masterSD"] = new Dictionary<string, int>();
            Checkpoints["masterWR"] = new Dictionary<string, int>();
            Checkpoints["masterGOTD"] = new Dictionary<string, int>();
            Checkpoints["masterSOTW"] = new Dictionary<string, int>();
            Checkpoints["masterD"] = new Dictionary<string, int>();
            Checkpoints["masterGOA"] = new Dictionary<string, int>();

            //make sure reset hasnt happened yet
            if (File.Exists(path + "\\resettimer.ini"))
            {
                long lastchecktime = DateTime.Now.AddYears(1).Ticks;
                try
                {
                    lastchecktime = long.Parse(File.ReadAllText(path + "\\resettimer.ini"));
                }
                catch
                {

                }

                //figure out when the next reset is after the last check, store it in temp.
                DateTime lastcheckeddatetime = DateTime.SpecifyKind(new DateTime(lastchecktime), DateTimeKind.Utc);
                DateTime temp = DateTime.SpecifyKind(new DateTime(lastchecktime), DateTimeKind.Utc);

                while (temp.Minute != 0)
                {
                    int gap = 60 - temp.Minute;
                    temp = temp.AddMinutes(gap);
                }
                while (temp.Hour != 17)
                {
                    temp = temp.AddHours(1);
                }
                while (temp.DayOfWeek != DayOfWeek.Tuesday)
                {
                    temp = temp.AddDays(1);
                }

                D2RESETTIME = temp; // this is used for detecting when to reset while everything is running.

                DateTime now = DateTime.Now.ToUniversalTime();

                //compare intended reset to now.
                if (temp > now)
                {
                    //reset didnt happen yet. update the recorded time.
                    File.Delete(path + "\\resettimer.ini");
                    File.WriteAllText(path + "\\resettimer.ini", now.Ticks.ToString());
                    //load all data
                    //save all as one big string. separate each activity with a _. separate each checkpoint with a ~, separate checkpoint from character index with a .
                    if (File.Exists(path + "\\checkpoints.ini"))
                    {
                        string output = File.ReadAllText(path + "\\checkpoints.ini");
                        foreach (string raid in output.Split("-"))
                        {
                            string raidname = "";
                            foreach (string checkpoint in raid.Split("~"))
                            {
                                if (raidname == "")
                                {
                                    raidname = checkpoint;
                                }
                                else
                                {
                                    string cpname = checkpoint.Split(".")[0];
                                    int cpindex = int.Parse(checkpoint.Split(".")[1]);
                                    if (checkpoint != "") Checkpoints[raidname].Add(cpname, cpindex);
                                }
                            }
                        }
                    }

                    if (File.Exists(path + "\\activities.ini")) //same formatting as above. each activity type is separated with an _, each activity is separated with a ~
                    {
                        int counter = 0;
                        string output = File.ReadAllText(path + "\\activities.ini");
                        foreach (string type in output.Split("_"))
                        {
                            foreach (string activity in type.Split("~"))
                            {
                                if (counter == 0) RaidActivityOrder.Add(activity);
                                if (counter == 1) DungeonActivityOrder.Add(activity);
                                if (counter == 2) PantheonActivityOrder.Add(activity);
                            }
                            counter++;
                        }
                        if (RaidActivityOrder.Count == 11 & DungeonActivityOrder.Count == 11 && PantheonActivityOrder.Count == 3) FlagGotActivityOrder = true;
                    }
                }
                else
                {
                    //reset has happened, need to wipe everything, and save the new time.
                    if (File.Exists(path + "\\checkpoints.ini")) File.Delete(path + "\\checkpoints.ini");
                    if (File.Exists(path + "\\activities.ini")) File.Delete(path + "\\activities.ini");
                    File.Delete(path + "\\resettimer.ini");
                    File.WriteAllText(path + "\\resettimer.ini", now.Ticks.ToString());

                    //figure out when next reset is to record that for later.
                    bool changed = false;
                    temp = DateTime.Now.ToUniversalTime();
                    while (temp.Minute != 0)
                    {
                        int gap = 60 - temp.Minute;
                        temp = temp.AddMinutes(gap);
                        changed = true;
                    }
                    while (temp.Hour != 17)
                    {
                        temp = temp.AddHours(1);
                        changed = true;
                    }
                    while (temp.DayOfWeek != DayOfWeek.Tuesday)
                    {
                        temp = temp.AddDays(1);
                        changed = true;
                    }
                    if (!changed) temp = temp.AddDays(7);

                    D2RESETTIME = temp;
                }

            }
            else
            {
                //never seen an update in my life. record the current time. if any checkpoints have been recorded something went horribly wrong and we cant trust it. delete them.
                DateTime now = DateTime.Now.ToUniversalTime();
                File.WriteAllText(path + "\\resettimer.ini", now.ToString());
                if (File.Exists(path + "\\checkpoints.ini")) File.Delete(path + "\\checkpoints.ini");
                if (File.Exists(path + "\\activities.ini")) File.Delete(path + "\\activities.ini");
            }
        } //TODO dont scrub checkpoints for activities that dont lose their checkpoints. 

        public static async void InitializeBot()
        {
            DiscordClient = new(new BotToken(DiscordDevToken), new GatewayClientConfiguration()
            {
                Intents = GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.MessageContent,
                Logger = new ConsoleLogger(),
            });

            // Add the handler to handle commands
            DiscordClient.MessageCreate += HandleMessages;

            //start the client
            await DiscordClient.StartAsync();
        }

        public static async void PrepCharMenu()
        {
            flagIntroSection = false;
            new Thread(async () =>
            {
                Thread.CurrentThread.IsBackground = true;

                UpdateStatusBar("Initializing... Waiting for character select menu.", UserStatusType.DoNotDisturb);

                statusheader = "Launch Conditions:";
                statussubtext = "Waiting for start menu.";
                UpdateTextDisplay();
                AwaitColorChange(95, 5, 2);
                statusheader = "Launch Conditions:";
                statussubtext = "Start menu located. Continuing to character select...";
                UpdateTextDisplay();
                Task.Delay(101).Wait();
                SetCursorPos(ConvertAspectRatioCoords(50, 50).X, ConvertAspectRatioCoords(50, 50).Y);
                SendClick(ConvertAspectRatioCoords(50, 50));
                Task.Delay(101).Wait();
                SetCursorPos(ConvertAspectRatioCoords(50, 50).X, ConvertAspectRatioCoords(50, 50).Y);
                SendClick(ConvertAspectRatioCoords(50, 50));
                Task.Delay(101).Wait();
                SendClick(ConvertAspectRatioCoords(50, 50));
                statusheader = "Launch Conditions:";
                statussubtext = "Waiting for character select...";
                UpdateTextDisplay();
                Task.Delay(1000).Wait();
                AwaitText("ExittoDesktop", ConvertAspectRatioCoords(5.15625, 95.972222222), ConvertAspectRatioCoords(14.0625, 98.75));
                Task.Delay(1000).Wait();

                InputSimulator sim = new InputSimulator();
                sim.Keyboard.KeyPress(VirtualKeyCode.ESCAPE);
                Task.Delay(101).Wait();
                sim.Keyboard.KeyPress(VirtualKeyCode.ESCAPE);
                Task.Delay(101).Wait();

                statusheader = "Launch Conditions:";
                statussubtext = "Character change menu found, checking if reset has happened since last launch.";
                UpdateTextDisplay();
                //AwaitColorChange(15.82, 29.17, 2); //coords for colors behind the player character.
                Task.Delay(3000).Wait();
                if (!FlagGotActivityOrder)
                {
                    UpdateStatusBar("Initializing... Grabbing activity order.", UserStatusType.DoNotDisturb);
                    GetToDirectorForActivityCoords();
                    UpdateStatusBar("Init, Checkpoint Cleanup...", UserStatusType.DoNotDisturb);
                    CleanCheckpoints();
                }
                FlagGotActivityOrder = true;
                flagOnCharSelect = true;
                INITIALIZING = false;

                UpdateStatusBar("Idling...", UserStatusType.Online);

                statusheader = "Idle...";
                statussubtext = "";
                UpdateTextDisplay();
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Now up and running. o7");

            }).Start();
        }

        public static async void GetToDirectorForActivityCoords()
        {
            statusheader = "Activity location detection:";
            statussubtext = "Step 1/4: Get to orbit.";
            SelectChar(1);

            //in orbit. get to director.
            statusheader = "Activity location detection:";
            statussubtext = "Step 2/4: Get to director.";
            UpdateTextDisplay();
            SelectDirector();

            //on director, get to portal
            statusheader = "Activity location detection:";
            statussubtext = "Step 3/4: Locate portal.";
            SelectPortal();
            Task.Delay(1000).Wait();

            statusheader = "Activity location detection:";
            statussubtext = "Step 4/4: Get images of activities for OCR.";
            UpdateTextDisplay();

            GetActivityText();
        }

        public static bool ConnectController()
        {
            try
            {
                ControllerClient = new ViGEmClient();
                Controller = ControllerClient.CreateXbox360Controller();

                Thread.Sleep(100);

                Controller.Connect();

                _connected = true;
                return true;
            }
            catch (Exception ex)
            {
                _connected = false;
                statusheader = "Cannot find ViGEmBus:";
                statussubtext = "Restarting in 10 seconds... If ViGEmBus isn't installed this will just loop forever.";
                UpdateTextDisplay();

                Task.Delay(10000).Wait();

                //ViGEmBus failed to launch. restarting.
                string appName = Assembly.GetEntryAssembly().GetName().Name;
                string loc = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                loc = loc + "\\" + appName + ".exe"; //if you dont do it this way it gives a .dll file instead.
                System.Diagnostics.Process.Start(loc);

                // Closes the current process
                Environment.Exit(0);

                return false;
            }
        }

        public static async void GetActivityText()
        {
            bool good = true;
            //convert to 16x9 for u.i. with a border for all math.
            int width = d2window.Right - d2window.Left;
            int height = d2window.Bottom - d2window.Top;
            double ratio = width / height;
            double desiredratio = 16 / 9;
            int leftbuffer = 0;
            int topbuffer = 0;

            double iconpercentageX = 21.68;
            double iconpercentageY = 6;

            double firstemblemX = 12.14;
            double firstemblemY = 31.94;

            double horgap = 4.69;
            double vertgap = 21.365;

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

            //find the start location of the first activity icon regardless of aspect ratio
            int starticonx = (int)Math.Round(width * (firstemblemX / 100)) + leftbuffer;
            int starticony = (int)Math.Round(height * (firstemblemY / 100)) + topbuffer;

            //get size of the icons for a given screen size
            int iconwidth = (int)Math.Round(width * (iconpercentageX / 100));
            int iconheight = (int)Math.Round(height * (iconpercentageY / 100));

            //get gap between icons regardless of aspect ratio and screen size
            int iconXgap = (int)Math.Round(width * (horgap / 100));
            int iconYgap = (int)Math.Round(height * (vertgap / 100));

            int ypos = starticony;
            int xpos = starticonx;

            List<Bitmap> raidimagelist = new List<Bitmap>();
            List<Bitmap> dungeonimagelist = new List<Bitmap>();
            List<Bitmap> pantheonimagelist = new List<Bitmap>();

            DungeonActivityOrder.Clear();
            RaidActivityOrder.Clear();
            PantheonActivityOrder.Clear();

            //change to controller input
            Controller.SetButtonState(Xbox360Button.A, true);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.A, false);

            Task.Delay(2000).Wait();

            //raids

            statusheader = "Activity OCR:";
            statussubtext = "Grabbing bitmaps of raids.";
            UpdateTextDisplay();

            for (int i = 0; i < 2; i++)
            {
                xpos = starticonx;
                for (int j = 0; j < 3; j++)
                {
                    Bitmap bmpScreenshot = new Bitmap(iconwidth, iconheight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    Graphics g = Graphics.FromImage(bmpScreenshot);
                    g.CopyFromScreen(xpos, ypos, 0, 0, new System.Drawing.Size(iconwidth, iconheight));
                    raidimagelist.Add(bmpScreenshot);
                    xpos = xpos + iconwidth + iconXgap;
                    Task.Delay(101).Wait();
                }
                ypos = ypos + iconheight + iconYgap;
            }

            Task.Delay(101).Wait();

            Controller.SetButtonState(Xbox360Button.Down, true);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.Down, false);
            ypos = starticony;

            AwaitColorChange(50, 50, 1);
            Task.Delay(2000).Wait();

            for (int i = 0; i < 2; i++)
            {
                xpos = starticonx;
                for (int j = 0; j < 3; j++)
                {
                    if (raidimagelist.Count == 11) break;
                    Bitmap bmpScreenshot = new Bitmap(iconwidth, iconheight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    Graphics g = Graphics.FromImage(bmpScreenshot);
                    g.CopyFromScreen(xpos, ypos, 0, 0, new System.Drawing.Size(iconwidth, iconheight));
                    raidimagelist.Add(bmpScreenshot);
                    xpos = xpos + iconwidth + iconXgap;
                    Task.Delay(100).Wait();
                }
                if (raidimagelist.Count == 11) break;
                ypos = ypos + iconheight + iconYgap;
            }

            //dungeons

            statusheader = "Activity OCR:";
            statussubtext = "Grabbing bitmaps of dungeons.";
            UpdateTextDisplay();

            Controller.SetButtonState(Xbox360Button.RightShoulder, true);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.RightShoulder, false);
            ypos = starticony;

            AwaitColorChange(50, 50, 1);
            Task.Delay(2000).Wait();

            for (int i = 0; i < 2; i++)
            {
                xpos = starticonx;
                for (int j = 0; j < 3; j++)
                {
                    Bitmap bmpScreenshot = new Bitmap(iconwidth, iconheight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    Graphics g = Graphics.FromImage(bmpScreenshot);
                    g.CopyFromScreen(xpos, ypos, 0, 0, new System.Drawing.Size(iconwidth, iconheight));
                    dungeonimagelist.Add(bmpScreenshot);
                    xpos = xpos + iconwidth + iconXgap;
                    Task.Delay(100).Wait();
                }
                ypos = ypos + iconheight + iconYgap;
            }
            Controller.SetButtonState(Xbox360Button.Down, true);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.Down, false);

            AwaitColorChange(50, 50, 1);
            Task.Delay(2000).Wait();

            ypos = starticony;

            for (int i = 0; i < 2; i++)
            {
                xpos = starticonx;
                for (int j = 0; j < 3; j++)
                {
                    if (dungeonimagelist.Count == 11) break;
                    Bitmap bmpScreenshot = new Bitmap(iconwidth, iconheight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    Graphics g = Graphics.FromImage(bmpScreenshot);
                    g.CopyFromScreen(xpos, ypos, 0, 0, new System.Drawing.Size(iconwidth, iconheight));
                    dungeonimagelist.Add(bmpScreenshot);
                    xpos = xpos + iconwidth + iconXgap;
                    Task.Delay(100).Wait();
                }
                if (dungeonimagelist.Count == 11) break;
                ypos = ypos + iconheight + iconYgap;
            }

            //pantheons

            statusheader = "Activity OCR:";
            statussubtext = "Grabbing bitmaps of pantheon activites.";
            UpdateTextDisplay();

            Controller.SetButtonState(Xbox360Button.RightShoulder, true);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.RightShoulder, false);

            AwaitColorChange(50, 50, 1);
            Task.Delay(2000).Wait();

            ypos = starticony + iconheight + iconYgap;
            xpos = starticonx;
            for (int j = 0; j < 3; j++)
            {
                //i = x, j = y
                Bitmap bmpScreenshot = new Bitmap(iconwidth, iconheight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Graphics g = Graphics.FromImage(bmpScreenshot);
                g.CopyFromScreen(xpos, ypos, 0, 0, new System.Drawing.Size(iconwidth, iconheight));
                pantheonimagelist.Add(bmpScreenshot);
                xpos = xpos + iconwidth + iconXgap;
                Task.Delay(100).Wait();
            }

            statusheader = "Activity OCR:";
            statussubtext = "Reading text of raids.";
            UpdateTextDisplay();

            int count = 0;
            foreach (Bitmap map in raidimagelist)
            {
                if (!good) break;
                map.ApplyEffect(new System.Drawing.Imaging.Effects.GrayScaleEffect());
                map.ApplyEffect(new System.Drawing.Imaging.Effects.BrightnessContrastEffect(-25, 0));
                map.ApplyEffect(new System.Drawing.Imaging.Effects.BrightnessContrastEffect(0, 100));
                map.ApplyEffect(new System.Drawing.Imaging.Effects.BlurEffect(2, false));
                map.ApplyEffect(new System.Drawing.Imaging.Effects.InvertEffect());
                string ocrstring = GetText(map).ToLower().Replace("(", "").Replace(")", "").Replace("'", "").Replace(":", "").Replace("\n", "");
                string output = ConvertOCRtoAbbreviation(ocrstring);
                map.Dispose();
                if (output == "fail")
                {
                    good = false;
                    break;
                }
                RaidActivityOrder.Add(output);
                Task.Delay(100).Wait();
                count++;
            }

            statusheader = "Activity OCR:";
            statussubtext = "Reading text of dungeons.";
            UpdateTextDisplay();

            count = 0;
            foreach (Bitmap map in dungeonimagelist)
            {
                if (!good) break;
                map.ApplyEffect(new System.Drawing.Imaging.Effects.GrayScaleEffect());
                map.ApplyEffect(new System.Drawing.Imaging.Effects.BrightnessContrastEffect(-25, 0));
                map.ApplyEffect(new System.Drawing.Imaging.Effects.BrightnessContrastEffect(0, 100));
                map.ApplyEffect(new System.Drawing.Imaging.Effects.BlurEffect(2, false));
                map.ApplyEffect(new System.Drawing.Imaging.Effects.InvertEffect());
                string ocrstring = GetText(map).ToLower().Replace("(", "").Replace(")", "").Replace("'", "").Replace(":", "").Replace("\n", "");
                string output = ConvertOCRtoAbbreviation(ocrstring);
                map.Dispose();
                if (output == "fail")
                {
                    good = false;
                    break;
                }
                DungeonActivityOrder.Add(output);
                Task.Delay(100).Wait();
                count++;
            }

            statusheader = "Activity OCR:";
            statussubtext = "Reading text of pantheon activites.";
            UpdateTextDisplay();

            count = 0;
            foreach (Bitmap map in pantheonimagelist)
            {
                if (!good) break;
                map.ApplyEffect(new System.Drawing.Imaging.Effects.GrayScaleEffect());
                map.ApplyEffect(new System.Drawing.Imaging.Effects.BrightnessContrastEffect(-25, 0));
                map.ApplyEffect(new System.Drawing.Imaging.Effects.BrightnessContrastEffect(0, 100));
                map.ApplyEffect(new System.Drawing.Imaging.Effects.BlurEffect(2, false));
                map.ApplyEffect(new System.Drawing.Imaging.Effects.InvertEffect());
                string ocrstring = GetText(map).ToLower().Replace("(", "").Replace(")", "").Replace("'", "").Replace(":", "").Replace("\n", "");
                string output = ConvertOCRtoAbbreviation(ocrstring);
                map.Dispose();
                if (output == "fail")
                {
                    good = false;
                    break;
                }
                PantheonActivityOrder.Add(output);
                Task.Delay(100).Wait();
                count++;
            }

            Thread.Sleep(101);
            Controller.SetButtonState(Xbox360Button.B, true);
            Thread.Sleep(101);
            Controller.SetButtonState(Xbox360Button.B, false);

            Thread.Sleep(101);
            Controller.SetButtonState(Xbox360Button.B, true);
            Thread.Sleep(101);
            Controller.SetButtonState(Xbox360Button.B, false);

            Thread.Sleep(101);
            Controller.SetButtonState(Xbox360Button.B, true);
            Thread.Sleep(101);
            Controller.SetButtonState(Xbox360Button.B, false);

            Thread.Sleep(101);
            Controller.SetButtonState(Xbox360Button.B, true);
            Thread.Sleep(101);
            Controller.SetButtonState(Xbox360Button.B, false);

            ReturnToCharSelectFast();

            if (good == false)
            {
                GetToDirectorForActivityCoords();
                return;
            }

            statusheader = "Activity OCR:";
            statussubtext = "Succeeded. Saving data to storage for next reset.";
            UpdateTextDisplay();

            //save the order of activities for the next reset
            string fileoutput = RaidActivityOrder.First(); //add the first one to make separators easier to deal with
            for (int i = 1; i < RaidActivityOrder.Count; i++) //start with the second one to make separators cleaner
            {
                fileoutput = fileoutput + "~" + RaidActivityOrder[i];
            }
            fileoutput = fileoutput + "_" + DungeonActivityOrder.First(); //add the activity split, and then same as before
            for (int i = 1; i < DungeonActivityOrder.Count; i++) //start with the second one to make separators cleaner
            {
                fileoutput = fileoutput + "~" + DungeonActivityOrder[i];
            }
            fileoutput = fileoutput + "_" + PantheonActivityOrder.First(); //add the activity split, and then same as before
            for (int i = 1; i < PantheonActivityOrder.Count; i++) //start with the second one to make separators cleaner
            {
                fileoutput = fileoutput + "~" + PantheonActivityOrder[i];
            }

            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (File.Exists(path + "\\activities.ini")) File.Delete(path + "\\activities.ini");

            File.WriteAllText(path + "\\activities.ini", fileoutput);
        }

        public static string ConvertOCRtoAbbreviation(string input)
        {
            switch (input)
            {
                case "crotasend":
                    return "CE";
                case "deepstonecrypt":
                    return "DSC";
                case "thedesertperpetualepic":
                    return "DPE";
                case "thedesertperpetual":
                    return "DP";
                case "salvationsedge":
                    return "SE";
                case "rootofnightmares":
                    return "RON";
                case "kingsfall":
                    return "KF";
                case "vowofthedisciple":
                    return "VOW";
                case "vaultofglass":
                    return "VOG";
                case "gardenofsalvation":
                    return "GOS";
                case "lastwish":
                    return "LW";
                case "warlordsruin":
                    return "WR";
                case "pitofheresy":
                    return "PIT";
                case "equilibrium":
                    return "EQ";
                case "sundereddoctrine":
                    return "SD";
                case "vespershost":
                    return "VH";
                case "ghostsofthedeep":
                    return "GOTD";
                case "spireofthewatcher":
                    return "SOTW";
                case "duality":
                    return "D";
                case "graspofavarice":
                    return "GOA";
                case "prophecy":
                    return "PR";
                case "theshatteredthrone":
                    return "ST";
                case "pantheoncalusresplendent":
                    return "CR";
                case "pantheonmorgethsurpassing":
                    return "MS";
                case "pantheoninsurrectionprimerevolutionary":
                    return "GAUNTLET";
            }
            return "fail";
        }
    }
}
