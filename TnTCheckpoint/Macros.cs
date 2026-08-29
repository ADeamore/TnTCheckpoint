using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using NetCord;
using NetCord.Gateway;
using WindowsInput;
using WindowsInput.Native;
using static TnTCheckpoint.ConstantsAndGlobals;
using static TnTCheckpoint.DebugCommunication;
using static TnTCheckpoint.DLLImportsStructsAndEnums;
using static TnTCheckpoint.ScreenspaceInteractionsAndReading;
using static TnTCheckpoint.Bookkeeping;
using Color = System.Drawing.Color;

namespace TnTCheckpoint
{
    public class Macros
    {
        public static void ReturnToCharSelectFast()
        {
            statussubtext = "Return to orbit fast: Making sure I'm taking controller input.";
            UpdateTextDisplay();

            int width = d2window.Right - d2window.Left;
            int height = d2window.Bottom - d2window.Top;

            int iconwidth = (int)Math.Round(width * 0.071484);
            int iconheight = (int)Math.Round(height * 0.025695);
            Point startcoords = ConvertAspectRatioCoords(35.195, 49.375);
            int xpos = startcoords.X;
            int ypos = startcoords.Y;

            //make sure im in controller mode
            Controller.SetButtonState(Xbox360Button.RightThumb, true);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.RightThumb, false);
            Task.Delay(101).Wait();

            statussubtext = "Return to orbit fast: Doing menuing to get back to character select...";
            UpdateTextDisplay();

            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.Start, true);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.Start, false);

            Task.Delay(500).Wait();
            SendClick(ConvertAspectRatioCoords(84.21, 23.26));
            Task.Delay(500).Wait();

            Point selectpos = ConvertAspectRatioCoords(89.2578125, 4.4444444444);
            SetCursorPos(selectpos.X, selectpos.Y);
            Task.Delay(101).Wait();
            SendClick(selectpos);

            Task.Delay(700).Wait();

            selectpos = ConvertAspectRatioCoords(14.8046875, 63.75);
            SetCursorPos(selectpos.X, selectpos.Y);
            Task.Delay(101).Wait();
            SendClick(selectpos);
            Task.Delay(101).Wait();

            SetCursorPos(ConvertAspectRatioCoords(84.21, 23.26).X, ConvertAspectRatioCoords(84.21, 23.26).Y);
            Task.Delay(101).Wait();

            for (int i = 0; i < 4; i++)
            {
                Controller.SetButtonState(Xbox360Button.A, true);
                Task.Delay(101).Wait();
                Controller.SetButtonState(Xbox360Button.A, false);
                Task.Delay(101).Wait();
            }

            Task.Delay(200).Wait();
            AwaitText("ExittoDesktop", ConvertAspectRatioCoords(0.5, 95.972222222), ConvertAspectRatioCoords(14.0625, 98.75));
            SetCursorPos(ConvertAspectRatioCoords(50, 50).X, ConvertAspectRatioCoords(50, 50).Y);
            SendClick(ConvertAspectRatioCoords(50, 50));
            Task.Delay(1000).Wait();
            flagOnCharSelect = true;

            statusheader = "Idle...";
            statussubtext = "";
            UpdateTextDisplay();
        }

        /// <summary>
        /// this assumes you're already on character select screen. Counts from 1, not 0
        /// </summary>
        /// <param name="charslot"></param>
        public static async void SelectChar(int charslot)
        {
            flagOnCharSelect = false;
            int gap = (int)Math.Round((charslot - 1) * 10.278);

            statussubtext = "Selecting character...";
            UpdateTextDisplay();

            SetCursorPos(ConvertAspectRatioCoords(60.859, 41.806 + gap).X, ConvertAspectRatioCoords(60.859, 41.806 + gap).Y);
            SendClick(ConvertAspectRatioCoords(60.859, 41.806 + gap));
            Task.Delay(101).Wait();
            SendClick(ConvertAspectRatioCoords(60.859, 41.806 + gap));
            Task.Delay(750).Wait();
            AwaitColorChange(50, 50, 2);
            Task.Delay(3000).Wait();
            //now in orbit
        }

        /// <summary>
        /// this assumes you're already in orbit.
        /// </summary>
        public static async void SelectDirector()
        {
            statussubtext = "Getting to director...";
            UpdateTextDisplay();

            //clear notification for iron banner
            Controller.SetButtonState(Xbox360Button.RightThumb, true); //this is to make sure im in controller mode
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.RightThumb, false);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.A, true);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.A, false);
            Task.Delay(101).Wait();

            //get to director
            Controller.SetButtonState(Xbox360Button.Back, true);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.Back, false);
            Task.Delay(101).Wait();
            AwaitColorChange(56.52, 87.08, 2);
            Task.Delay(2500).Wait();

            //double down on clearing notification for iron banner
            Controller.SetButtonState(Xbox360Button.A, true);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.A, false);
            Task.Delay(101).Wait();
            SendClick(new Point(50, 50)); //swap back to mnk
            Task.Delay(500).Wait();
        }

        /// <summary>
        /// this assumes you're already on the director.
        /// </summary>
        public static async void SelectPortal()
        {
            statussubtext = "Locating portal...";
            UpdateTextDisplay();

            SendClick(ConvertAspectRatioCoords(50, 50));
            Task.Delay(101).Wait();
            SendClick(ConvertAspectRatioCoords(50, 50));
            Task.Delay(101).Wait();
            SetCursorPos(ConvertAspectRatioCoords(58, 87.08).X, ConvertAspectRatioCoords(58, 87.08).Y);
            Task.Delay(101).Wait();
            SendClick(ConvertAspectRatioCoords(56.52, 87.08));
            AwaitColorChange(50, 85, 2);
            Task.Delay(1000).Wait();
            //now on portal
        }

        /// <summary>
        /// this assumes you're on the portal, on the first page.
        /// </summary>
        /// <param name="act"></param>
        public static async void SelectActivity(string act)
        {
            //change to controller input
            Controller.SetButtonState(Xbox360Button.RightThumb, true);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.RightThumb, false);
            Controller.SetAxisValue(Xbox360Axis.LeftThumbY, STICK_BACK);
            Task.Delay(1000).Wait();
            Controller.SetAxisValue(Xbox360Axis.LeftThumbY, STICK_CENTER);
            Task.Delay(500).Wait();

            //statussubtext = "Navitgating to activity..."; 
            //UpdateTextDisplay();

            int screenloc = 0;

            //get which list the activity belongs to
            if (RaidActivityOrder.Contains(act))
            {
                screenloc = RaidActivityOrder.IndexOf(act);

                if (RaidActivityOrder.IndexOf(act) > 5)
                {
                    screenloc = RaidActivityOrder.IndexOf(act) - 6;

                    Controller.SetButtonState(Xbox360Button.Down, true);
                    Task.Delay(101).Wait();
                    Controller.SetButtonState(Xbox360Button.Down, false);
                    Task.Delay(2000).Wait();
                }
            }
            if (DungeonActivityOrder.Contains(act))
            {
                screenloc = DungeonActivityOrder.IndexOf(act);
                Controller.SetButtonState(Xbox360Button.RightShoulder, true);
                Task.Delay(101).Wait();
                Controller.SetButtonState(Xbox360Button.RightShoulder, false);
                Task.Delay(2000).Wait();

                if (DungeonActivityOrder.IndexOf(act) > 5)
                {
                    screenloc = DungeonActivityOrder.IndexOf(act) - 6;

                    Controller.SetButtonState(Xbox360Button.Down, true);
                    Task.Delay(101).Wait();
                    Controller.SetButtonState(Xbox360Button.Down, false);
                    Task.Delay(2000).Wait();
                }
            }

            if (PantheonActivityOrder.Contains(act))
            {
                screenloc = PantheonActivityOrder.IndexOf(act) + 3;
                Controller.SetButtonState(Xbox360Button.RightShoulder, true);
                Task.Delay(101).Wait();
                Controller.SetButtonState(Xbox360Button.RightShoulder, false);
                Task.Delay(500).Wait();
                Controller.SetButtonState(Xbox360Button.RightShoulder, true);
                Task.Delay(101).Wait();
                Controller.SetButtonState(Xbox360Button.RightShoulder, false);
                Task.Delay(2000).Wait();
            }

            //flick to its location and click it.
            Point selectpos = ConvertAspectRatioCoords(25, 45.5);
            int horgap = ConvertAspectRatioCoords(50, 0).X - selectpos.X;
            int vertgap = ConvertAspectRatioCoords(0, 75).Y - selectpos.Y;
            if (screenloc > 5) screenloc = screenloc - 6;
            if (screenloc > 2)
            {
                screenloc = screenloc - 3;
                selectpos.Y = selectpos.Y + vertgap;
                selectpos.X = selectpos.X + horgap * screenloc;
            }
            else
            {
                selectpos.X = selectpos.X + horgap * screenloc;
            }

            SendClick(selectpos);
            Task.Delay(100).Wait();

            SetCursorPos(selectpos.X, selectpos.Y);
            Task.Delay(100).Wait();
            SendClick(selectpos);
            SetCursorPos(ConvertAspectRatioCoords(50, 50).X, ConvertAspectRatioCoords(50, 50).Y);
            AwaitColorChange(85.9375, 83.33333333, 1);
            Task.Delay(1000).Wait();
        }

        /// <summary>
        /// assumes you're already on the activity that has the checkpoint icon.
        /// </summary>
        public static void RemoveCheckpoint()
        {
            //highlight over the play button just to have a consistent location for the checkpoint on screen.
            SetCursorPos(ConvertAspectRatioCoords(75.117, 83.75).X, ConvertAspectRatioCoords(75.117, 83.75).Y);

            Task.Delay(200).Wait();

            Color checkpoint = GetColorAt(ConvertAspectRatioCoords(66.640625, 77.0138889));


            int avg = checkpoint.R + checkpoint.G + checkpoint.B;
            avg = avg / 3;

            List<int> colorlist = new List<int>();
            colorlist.Add(checkpoint.R);
            colorlist.Add(checkpoint.G);
            colorlist.Add(checkpoint.B);
            colorlist.Sort();
            int gap = colorlist.Last() - colorlist.First();

            if (avg > 200 & gap < 10) //making sure its some level of white
            {
                //i got the checkpoint

                Point cursorpos = ConvertAspectRatioCoords(66.71875, 77.4305556);

                SetCursorPos(cursorpos.X, cursorpos.Y);
                Task.Delay(101).Wait();
                SendClick(cursorpos);
                Task.Delay(101).Wait();
                SetCursorPos(cursorpos.X, cursorpos.Y);
                Task.Delay(101).Wait();
                SendClick(cursorpos);
                Task.Delay(101).Wait();

                Controller.SetButtonState(Xbox360Button.LeftThumb, true);
                Task.Delay(101).Wait();
                Controller.SetButtonState(Xbox360Button.LeftThumb, true);
                Task.Delay(500).Wait();

                Controller.SetButtonState(Xbox360Button.X, true);
                Task.Delay(3000).Wait();
                Controller.SetButtonState(Xbox360Button.X, false);
                Task.Delay(101).Wait();
            }

            Controller.SetButtonState(Xbox360Button.LeftThumb, true);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.LeftThumb, false);
            Task.Delay(1000).Wait();
        }

        /// <summary>
        /// assumes you're already on the activity page
        /// </summary>
        public static async void SelectMaster()
        {
            statussubtext = "Swapping to master difficulty...";
            UpdateTextDisplay();

            Point selectpos = ConvertAspectRatioCoords(86.29, 77.5694);
            SetCursorPos(selectpos.X, selectpos.Y);
            Task.Delay(101).Wait();
            SendClick(selectpos);
            AwaitColorChange(85.9375, 83.33333333, 1);

            selectpos = ConvertAspectRatioCoords(13.6719, 29.16666667);
            SetCursorPos(selectpos.X, selectpos.Y);
            Task.Delay(2000).Wait();
            AwaitColorChange(39.9609375, 49.23611111111, 1);
            Task.Delay(1000).Wait();
            SendClick(selectpos);
            Task.Delay(1000).Wait();

            Controller.SetButtonState(Xbox360Button.B, true);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.B, false);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.B, true);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.B, false);

            AwaitColorChange(85.9375, 83.33333333, 1);
            SendClick(selectpos);
            Task.Delay(1000).Wait();
        }

        /// <summary>
        /// assumes you're already on the activity page
        /// </summary>
        public static async void SelectFeats(List<string> feats)
        {
            statussubtext = "Selecting feats...";
            UpdateTextDisplay();

            Point selectpos = ConvertAspectRatioCoords(86.29, 77.5694);
            SetCursorPos(selectpos.X, selectpos.Y);
            Task.Delay(101).Wait();
            SendClick(selectpos);
            AwaitColorChange(33.7890625, 25.8333333, 1);

            selectpos = ConvertAspectRatioCoords(33.7890625, 25.8333333);
            SetCursorPos(selectpos.X, selectpos.Y);
            Task.Delay(100).Wait();
            AwaitColorChange(33.7109375, 37.0833333, 1);
            Task.Delay(1000).Wait();

            int i = 0;
            double gap = 5.2;
            foreach (string f in feats)
            {
                //"token","phase","battalions","challenges","cutthroat"
                double featslot = 0;
                if (f == "token") featslot = gap;
                if (f == "phase") featslot = gap * 2;
                if (f == "battalions") featslot = gap * 3;
                if (f == "challenges") featslot = gap * 4;
                if (f == "cutthroat") featslot = gap * 5;

                selectpos = ConvertAspectRatioCoords(33.7890625 + (gap * i), 25.8333333);
                SetCursorPos(selectpos.X, selectpos.Y);
                Task.Delay(200).Wait();
                selectpos = ConvertAspectRatioCoords(33.7890625 + featslot, 36.52777777);
                SetCursorPos(selectpos.X, selectpos.Y);
                Task.Delay(200).Wait();
                SendClick(selectpos);
                Task.Delay(200).Wait();
                i++;
            }

            Task.Delay(1000).Wait();

            Controller.SetButtonState(Xbox360Button.B, true);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.B, false);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.B, true);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.B, false);

            AwaitColorChange(85.9375, 83.33333333, 1);
            SendClick(selectpos);
            Task.Delay(1000).Wait();
        }

        /// <summary>
        /// assumes you're already on the activity page
        /// </summary>
        public static async void ClearFeats()
        {
            statussubtext = "Selecting feats...";
            UpdateTextDisplay();

            Point selectpos = ConvertAspectRatioCoords(86.29, 77.5694);
            SetCursorPos(selectpos.X, selectpos.Y);
            Task.Delay(101).Wait();
            SendClick(selectpos);
            AwaitColorChange(33.7890625, 25.8333333, 1);

            selectpos = ConvertAspectRatioCoords(33.7890625, 25.8333333);
            SetCursorPos(selectpos.X, selectpos.Y);
            Task.Delay(100).Wait();
            AwaitColorChange(33.7109375, 37.0833333, 1);
            Task.Delay(1000).Wait();

            double gap = 5.2;
            for (int i = 0; i < 5; i++)
            {
                //"token","phase","battalions","challenges","cutthroat"

                selectpos = ConvertAspectRatioCoords(33.7890625 + (gap * i), 25.8333333);
                SetCursorPos(selectpos.X, selectpos.Y);
                Task.Delay(200).Wait();
                selectpos = ConvertAspectRatioCoords(33.7890625, 36.52777777);
                SetCursorPos(selectpos.X, selectpos.Y);
                Task.Delay(200).Wait();
                SendClick(selectpos);
                Task.Delay(200).Wait();
            }

            Task.Delay(1000).Wait();

            Controller.SetButtonState(Xbox360Button.B, true);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.B, false);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.B, true);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.B, false);

            AwaitColorChange(85.9375, 83.33333333, 1);
            SendClick(selectpos);
            Task.Delay(1000).Wait();
        }

        public static async void InvitePlayer(string chat)
        {
            statussubtext = "Invitingn to fireteam...";
            UpdateTextDisplay();

            Task.Delay(1000).Wait();

            InputSimulator sim = new InputSimulator();
            sim.Keyboard.KeyPress(VirtualKeyCode.BACK);
            Task.Delay(101).Wait();
            sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            Task.Delay(1000).Wait();
            sim.Keyboard.TextEntry(chat);
            Task.Delay(1000).Wait();
            sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
        }

        public static bool JoinFireteamFromOrbit(string chat, string activ)
        {
            statussubtext = "Typing in fireteam name...";
            UpdateTextDisplay();

            Task.Delay(1000).Wait();

            InputSimulator sim = new InputSimulator();
            sim.Keyboard.KeyPress(VirtualKeyCode.BACK);
            Task.Delay(101).Wait();
            sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            Task.Delay(1000).Wait();
            sim.Keyboard.TextEntry(chat);
            Task.Delay(1000).Wait();
            sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

            statussubtext = "Checking if theres an error code...";
            UpdateTextDisplay();

            bool works = false;
            DateTime breaktime = DateTime.Now.AddSeconds(10);
            while (true)
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
                if (ocrstring.Contains("pleasewait")) works = true;
                if (ocrstring.Contains("unable")) works = false;
                if (DateTime.Now > breaktime) break;
            }

            if (!works)
            {
                statussubtext = "Error code detected :(";
                UpdateTextDisplay();
                return false;
            }
            else
            {
                //joining
                statussubtext = "Join successful. Detecting first black screen...";
                UpdateTextDisplay();

                WaitThruBlackscreens(activ);

                Task.Delay(3000).Wait();

                statussubtext = "Join successful. Now boots on ground...";
                UpdateTextDisplay();

                flagBootsOnGround = true;

                return true;
            }
        } //TODO check if more activities skip second blackscreen on join

        public static bool JoinFireteamInOrbit(string chat)
        {
            statussubtext = "Typing in fireteam name...";
            UpdateTextDisplay();

            Task.Delay(1000).Wait();

            InputSimulator sim = new InputSimulator();
            sim.Keyboard.KeyPress(VirtualKeyCode.BACK);
            Task.Delay(101).Wait();
            sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            Task.Delay(1000).Wait();
            sim.Keyboard.TextEntry(chat);
            Task.Delay(1000).Wait();
            sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

            statussubtext = "Checking if theres an error code...";
            UpdateTextDisplay();

            bool works = false;
            DateTime breaktime = DateTime.Now.AddSeconds(10);
            while (true)
            {
                string ocrstring = CheckIfOrbitTextBox();

                if (ocrstring.Contains("pleasewait")) works = true;
                if (ocrstring.Contains("unable")) works = false;
                if (DateTime.Now > breaktime) break;
            }

            return works;
        }

        public static void NavigateToActivityFromCharSelect(int charslot, string activity, bool master)
        {
            SelectChar(charslot);
            SelectDirector();
            SelectPortal();
            SelectActivity(activity);
            if (master) SelectMaster();
        }

        public static bool VerifyCheckpointAndSave(int charslot, CommandLayout output)
        {

            NavigateToActivityFromCharSelect(charslot, output.activity, output.master);

            Task.Delay(2000).Wait();

            //highlight over the play button just to have a consistent location for the checkpoint on screen.
            SetCursorPos(ConvertAspectRatioCoords(75.117, 83.75).X, ConvertAspectRatioCoords(75.117, 83.75).Y);

            Task.Delay(1000).Wait();

            Color checkpoint = GetColorAt(ConvertAspectRatioCoords(66.640625, 77.0138889));

            int avg = checkpoint.R + checkpoint.G + checkpoint.B;
            avg = avg / 3;

            List<int> colorlist = new List<int>();
            colorlist.Add(checkpoint.R);
            colorlist.Add(checkpoint.G);
            colorlist.Add(checkpoint.B);
            colorlist.Sort();
            int gap = colorlist.Last() - colorlist.First();

            if (avg > 200 & gap < 10) //making sure its some level of white
            {
                //i got the checkpoint
                Checkpoints[output.activitykey].Add(output.checkpointname, charslot);
                SaveCheckpoints();
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Checkpoint grabbed successfully. Returning to orbit.");
                return true;
            }
            else
            {
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "It looks like the checkpoint failed to grab. Please try the command again or use a different method of getting a checkpoint.");
                return false;
            }
        }

        public static void CleanCheckpoints()
        {
            DELETINGCHECKPOINT = true;

            List<string> order = new List<string>();
            order.AddRange(RaidActivityOrder);
            order.AddRange(DungeonActivityOrder);
            order.AddRange(PantheonActivityOrder);

            foreach (string activity in Checkpoints.Keys)
            {
                if (activity.Contains("master"))
                {
                    int indx = order.IndexOf(activity.Replace("master", ""));
                    order.Insert(indx + 1, activity);
                }
            }

            for (int i = 1; i <= 3; i++)
            {
                SelectChar(i);
                SelectDirector();
                SelectPortal();
                int indx = 0;
                int page = 0;

                List<string> activ = order;

                foreach (string activity in Checkpoints.Keys)
                {
                    foreach (int output in Checkpoints[activity].Values)
                    {
                        if (output == i)
                        {
                            activ.Remove(activity);
                        }
                    }
                }

                foreach (string activity in activ)
                {
                    statussubtext = "Clearing out " + activity;
                    UpdateTextDisplay();
                    if (INITIALIZING) UpdateStatusBar("Initializing... Cleaning up character slot " + i + " on activity " + activ.IndexOf(activity) + "/" + activ.Count(), UserStatusType.DoNotDisturb);
                    if (!INITIALIZING) UpdateStatusBar("Cleaning up character slot " + i + " on activity " + activ.IndexOf(activity) + "/" + activ.Count(), UserStatusType.Idle);

                    string tmp = activity.Replace("master", "");

                    //this whole block makes sure I select the right page, and then tricks selectactivity into thinking im on a different page.
                    if (RaidActivityOrder.Contains(tmp))
                    {
                        indx = RaidActivityOrder.IndexOf(tmp);
                        if (indx > 5)
                        {
                            if (page != 1)
                            {
                                page = 1;
                            }
                            else
                            {
                                tmp = RaidActivityOrder[indx - 6];
                            }
                        }
                    }
                    if (DungeonActivityOrder.Contains(tmp))
                    {
                        if (page < 2)
                        {
                            indx = DungeonActivityOrder.IndexOf(tmp);
                            page = 2;
                        }
                        else
                        {
                            indx = DungeonActivityOrder.IndexOf(tmp);
                            if (indx > 5)
                            {
                                if (page != 3)
                                {
                                    page = 3;
                                    tmp = RaidActivityOrder[indx];
                                }
                                else
                                {
                                    tmp = RaidActivityOrder[indx - 6];
                                }
                            }
                            else
                            {
                                tmp = RaidActivityOrder[indx];
                            }
                        }
                    }
                    if (PantheonActivityOrder.Contains(tmp))
                    {
                        indx = PantheonActivityOrder.IndexOf(tmp) + 3;
                        if (page < 2)
                        {
                            page = 5;
                        }
                        else if (page < 4)
                        {
                            tmp = DungeonActivityOrder[indx];
                            page = 5;
                        }
                        else
                        {
                            tmp = RaidActivityOrder[indx];
                        }
                    }

                    SelectActivity(tmp);
                    if (activity.Contains("master")) SelectMaster();
                    SendClick(new Point(50, 50));

                    RemoveCheckpoint();
                    Task.Delay(101).Wait();

                    Controller.SetButtonState(Xbox360Button.B, true);
                    Task.Delay(101).Wait();
                    Controller.SetButtonState(Xbox360Button.B, false);
                    Task.Delay(101).Wait();
                }

                Controller.SetButtonState(Xbox360Button.B, true);
                Task.Delay(101).Wait();
                Controller.SetButtonState(Xbox360Button.B, false);
                Task.Delay(101).Wait();

                Controller.SetButtonState(Xbox360Button.B, true);
                Task.Delay(101).Wait();
                Controller.SetButtonState(Xbox360Button.B, false);
                Task.Delay(101).Wait();

                Controller.SetButtonState(Xbox360Button.B, true);
                Task.Delay(101).Wait();
                Controller.SetButtonState(Xbox360Button.B, false);
                Task.Delay(101).Wait();

                Controller.SetButtonState(Xbox360Button.B, true);
                Task.Delay(101).Wait();
                Controller.SetButtonState(Xbox360Button.B, false);
                Task.Delay(101).Wait();

                ReturnToCharSelectFast();
            }
        }

        public static void SpamLaunchButtonUntilWorks()
        {
            bool change = false;
            Point pointcheck = ConvertAspectRatioCoords(95.859, 83.75);
            Color spotcolor = GetColorAt(pointcheck);
            Point pointclick = ConvertAspectRatioCoords(75.117, 83.75);

            while (!change)
            {
                Color spotcolor2 = GetColorAt(pointcheck);

                if (Math.Abs(spotcolor.G - spotcolor2.G) > 80)
                {
                    if (spotcolor2.G != 0) change = true;
                }

                SendClick(pointclick);

                Task.Delay(250).Wait();
            }
        }

        public static void VerifyControllerInput()
        {
            Controller.SetButtonState(Xbox360Button.RightThumb, true);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.RightThumb, false);
            Task.Delay(101).Wait();
        }

        public static void NavigateToCollections()
        {
            //start, lb, lb, click lore tab
            Controller.SetButtonState(Xbox360Button.Start, true);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.Start, false);
            Task.Delay(101).Wait();
            Controller.SetAxisValue(Xbox360Axis.LeftThumbY, STICK_BACK);
            Task.Delay(1000).Wait();

            Controller.SetButtonState(Xbox360Button.LeftShoulder, true);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.LeftShoulder, false);
            Task.Delay(400).Wait();
            Controller.SetAxisValue(Xbox360Axis.LeftThumbY, STICK_CENTER);
            Task.Delay(101).Wait();

            Controller.SetButtonState(Xbox360Button.LeftShoulder, true);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.LeftShoulder, false);
            Task.Delay(1000).Wait();

            SendClick(new Point(0, 0));
            Task.Delay(1000).Wait();
            SetCursorPos(ConvertAspectRatioCoords(68.395375, 60).X, ConvertAspectRatioCoords(68.395375, 60).Y);
            Task.Delay(1000).Wait();

            VerifyControllerInput();
            Controller.SetButtonState(Xbox360Button.A, true);
            Task.Delay(101).Wait();
            Controller.SetButtonState(Xbox360Button.A, false);
            Task.Delay(101).Wait();
        }

        public static bool SingleAfkCycle()
        {
            Controller.SetButtonState(Xbox360Button.LeftShoulder, true);
            Task.Delay(200).Wait();
            Controller.SetButtonState(Xbox360Button.LeftShoulder, false);
            Task.Delay(3000).Wait();
            if (!HOLDINGLOAD) return false;

            Controller.SetButtonState(Xbox360Button.RightShoulder, true);
            Task.Delay(200).Wait();
            Controller.SetButtonState(Xbox360Button.RightShoulder, false);
            Task.Delay(3000).Wait();
            if (!HOLDINGLOAD) return false;
            return true;
        }
    }
}
