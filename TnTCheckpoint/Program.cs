using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using NetCord;
using NetCord.Gateway;
using NetCord.Logging;
using Tesseract;
using WindowsInput;
using WindowsInput.Native;
using Color = System.Drawing.Color;
using Message = NetCord.Gateway.Message;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

//TODO go thru everything and add status updates to the bot
//TODO no delays less than 67 ms so that it can work at 15fps and not drop inputs.

namespace TnTCheckpoint
{
    class TnTCheckpoint
    {
        #region dll imports

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("User32.Dll")]
        public static extern long SetCursorPos(int x, int y);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(
            [In] uint dwFlags,
            [In] uint dx,
            [In] uint dy,
            [In] int dwData,
            [In] uint dwExtraInfo);

        class Keyboard
        {
            [DllImport("user32.dll")]
            static extern short GetAsyncKeyState(int vKey);

            public static bool IsPressed(int key) => (GetAsyncKeyState(key) & 0x8000) != 0;
        }

        #endregion

        #region structs and enums

        public enum MouseEvents
        {
            MOUSEEVENTF_LEFTDOWN = 0x02,
            MOUSEEVENTF_LEFTUP = 0x04,
            MOUSEEVENTF_RIGHTDOWN = 0x08,
            MOUSEEVENTF_RIGHTUP = 0x10,
            MOUSEEVENTF_WHEEL = 0x0800,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        #endregion

        #region controllerconstants
        private const int VK_OEM_3 = 0xC0;

        private const short STICK_CENTER = 0;
        private const short STICK_BACK = short.MinValue;
        private const short STICK_FORWARD = short.MaxValue;
        private const short STICK_LEFT = short.MinValue;
        private const short STICK_RIGHT = short.MaxValue;
        public const byte TRIGGER_PULLED = byte.MaxValue;
        public const byte TRIGGER_RELEASED = byte.MinValue;

        #endregion

        public static RECT d2window = new RECT();
        private static Process D2Process;
        private static bool startup = true;
        private static bool foundd2 = false;

        private static Color black = Color.Black;

        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        private static ViGEmClient? _client;
        private static IXbox360Controller? _controller;

        private static readonly SemaphoreSlim _toggleLock = new(1, 1);
        private static bool _connected = false;
        private static GatewayClient client;

        public static bool holdingload = false;
        public static bool checkpointfarmmode = false;
        public static bool afkcycle = false;
        public static bool bootsonground = false;
        public static bool oncharselect = false;
        public static bool grabbingcheckpoint = false;
        public static int verifylevel = 0;
        public static bool verifying = false;
        public static bool transferingcheckpoint = false;
        public static bool deletingcheckpoint = false;
        public static DateTime ResetTime = DateTime.MaxValue;

        public static bool initializing = true;

        public static string statusheader = "";
        public static string statussubtext = "";
        public static int visualupdates = 0;
        public static int visualupdatestotal = 0;
        public static int visualupdatesx = 0;
        public static int visualupdatesy = 0;
        public static bool runningupdatedetection = false;

        public static CancellationToken OrbitToken = new CancellationToken();
        public static CancellationTokenSource OrbitTokenSource = new CancellationTokenSource();

        public static bool IntroSection = true;
        public static bool GottenActivityOrder = false;

        public static string workingusername = "";
        public static string workingdiscordname = "";
        public static int farmmode = 0;
        public static string activityname = "";
        public static string checkpointname = "";

        public static string DeveloperToken = ""; 
        public static double ChannelID = 0; 

        public static List<string> RaidActivityOrder = new List<string>();
        public static List<string> DungeonActivityOrder = new List<string>();
        public static List<string> PantheonActivityOrder = new List<string>();

        public static Dictionary<string, Dictionary<string, int>> checkpoints = new Dictionary<string, Dictionary<string, int>>();

        public static DateTime closetime = DateTime.MaxValue;

        public static bool closebuttonpressed = false;

        public const short FARMMODE_RAID = 0;
        public const short FARMMODE_DUNGEON = 1;
        public const int FARMMODE_PANTHEON = 2;

        public const string emptybar = "                                                                                                                                          ";

        protected static void UpdateTextDisplay()
        {
            Console.SetCursorPosition(0, 0);
            Console.Write(statusheader + emptybar);
            Console.SetCursorPosition(0, 1);
            Console.Write("     " + statussubtext + emptybar);
            Console.SetCursorPosition(0, 3);
            Console.Write("Color Detection: " + runningupdatedetection.ToString() + emptybar);
            Console.SetCursorPosition(0, 4);
            Console.Write("     Update count: " + visualupdates + "/" + visualupdatestotal + emptybar);
            Console.SetCursorPosition(0, 5);
            Console.Write("      At location: " + visualupdatesx + "," + visualupdatesy + emptybar);
            Console.SetCursorPosition(0, 6);
            Console.Write("You may hold Right Alt for 5 seconds at any time to kill the macro and Destiny2." + emptybar);
            for (int i = 7; i < 15; i++)
            {
                Console.SetCursorPosition(0, i);
                Console.Write($"{"\r".PadRight(Console.BufferWidth)}\r");
            }
            Console.SetCursorPosition(0, 7);
        }

        public static async void UpdateStatusBar(string status, UserStatusType stat)
        {
            await client.UpdatePresenceAsync(
                new PresenceProperties(stat).WithActivities([new UserActivityProperties("ignored", UserActivityType.Custom).WithState(status)] )
            );
        }

        static void Main()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            //check if I have a channel ID and dev token to load to run the bot. Otherwise this is first time setup and I should do that.
            //ask for dev token, and channel ID. Parse the channel ID and make sure its a valid double.
            //if its not first time boot, ask if i want to reconfigure, or if i want to launch.

            bool works = false;
            while (!works)
            {
                Console.Clear();
                Console.SetCursorPosition(0, 0);

                if (!File.Exists(path + "\\configuration.ini"))
                {
                    //first time boot
                    Console.WriteLine("What is your discord bot's Developer Token?\nThis should be a quite long string of random numbers, letters, and symbols.\nIf you don't have one type \"botsetup\" to open the website where you can set up your discord bot.");
                    string resp = Console.ReadLine();
                    if (resp.ToLower() == "botsetup")
                    {
                        Process p = new Process();
                        ProcessStartInfo ps = new ProcessStartInfo();
                        ps.FileName = "https://discord.com/developers/home";
                        ps.UseShellExecute = true;
                        p.StartInfo = ps;
                        p.Start();
                    }
                    else
                    {
                        DeveloperToken = resp;
                    }
                    bool channelgotten = false;
                    while (!channelgotten)
                    {
                        //channel id's are 17-19 didgets.
                        Console.Clear();
                        Console.SetCursorPosition(0, 0);
                        Console.WriteLine("What channel should I listen to in your server?\n\nThis should be a 17-19 didget number.\nFor detailed instructions on how to get it, please type \"botsetup\".");
                        resp = Console.ReadLine();
                        if (resp.ToLower() == "botsetup")
                        {
                            Console.Clear();
                            Console.SetCursorPosition(0, 0);
                            Console.WriteLine(" - Go into your discord settings using the gear icon in the bottom left corner of the window. \n - Scroll to the bottom to find the tab labelled \"developer\" and make sure developer mode is turned on.\n - Then right click on the channel you'd like me to listen to, and select \"Copy Channel ID\".\n\nPress enter to continue.");
                            Console.ReadLine();
                        }
                        else
                        {
                            long temp = 0;
                            try
                            {
                                temp = long.Parse(resp);
                            }
                            catch
                            {
                                //wasnt a number. we run it back.
                            }
                            if (temp.ToString().Length >= 17 & temp.ToString().Length <= 19)
                            {
                                //channel ID passes the test.
                                channelgotten = true;
                                works = true;
                                ChannelID = temp;

                                File.WriteAllText(path + "\\configuration.ini", DeveloperToken + "\n" + temp);
                            }
                        }
                    }
                }
                else
                {
                    //not first time boot

                    string resp = "";
                    //restarting due to reset or a crash
                    if (File.Exists(path + "\\reset.ini"))
                    {
                        File.Delete(path + "\\reset.ini");
                        resp = "launch";
                    }
                    else
                    {
                        //starting naturally
                        Console.WriteLine("Would you like to launch the bot? Or would you like to reconfigure it?\n(launch/reconfig):");
                        resp = Console.ReadLine();
                    }

                    if(resp.ToLower() == "launch")
                    {
                        string[] strings = File.ReadAllLines(path + "\\configuration.ini");
                        if(strings.Length != 2)
                        {
                            File.Delete(path + "\\configuration.ini");
                            continue;
                        }
                        DeveloperToken = strings[0];
                        long temp = 0;
                        try
                        {
                            temp = long.Parse(strings[1]);
                        }
                        catch
                        {
                            //couldnt parse. we bail.
                            File.Delete(path + "\\configuration.ini");
                            continue;
                        }
                        if (temp.ToString().Length >= 17 & temp.ToString().Length <= 19)
                        {
                            //channel ID passes the test.
                            works = true;
                            ChannelID = temp;
                        }
                        else
                        {
                            //channel id fails. we bail and restart.
                            File.Delete(path + "\\configuration.ini");
                            continue;
                        }
                    }
                    if(resp.ToLower() == "reconfig")
                    {
                        File.Delete(path + "\\configuration.ini");
                        continue;
                    }
                }
            }

            new Thread(async () =>
            {
                bool next = false;

                next = ConnectController();

                if (next)
                {
                    InitializeBot();

                    UpdateStatusBar("Initializing...", UserStatusType.DoNotDisturb);

                    InitializeCheckpoints();

                    new Thread(async () =>
                    {
                        Thread.CurrentThread.IsBackground = true;
                        string oldstatus = "";
                        while (true)
                        {
                            try
                            {
                                D2Process = Process.GetProcessesByName("destiny2").First();
                            }
                            catch
                            {
                                D2Process = null;
                            }

                            if (D2Process == null)
                            {
                                if (startup)
                                {
                                    startup = false;
                                    statusheader = "Launch Conditions:";
                                    statussubtext = "Launching Destiny...";
                                    UpdateTextDisplay();
                                    string strCmdText = "/C start steam://rungameid/1085660";
                                    Process.Start("CMD.exe", strCmdText).WaitForExit();
                                }
                                if (!IntroSection)
                                {
                                    //game crashed
                                    // Starts a new instance of the program itself
                                    string appName = Assembly.GetEntryAssembly().GetName().Name;
                                    string loc = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                                    if (!File.Exists(loc + "\\reset.ini")) File.Create(loc + "\\reset.ini");

                                    loc = loc + "\\" + appName + ".exe"; //if you dont do it this way it gives a .dll file instead.
                                    System.Diagnostics.Process.Start(loc); 

                                    // Closes the current process
                                    Environment.Exit(0);
                                }

                            }
                            else
                            {
                                if (startup)
                                {
                                    statusheader = "Launch Conditions:";
                                    statussubtext = "Restarting destiny for initialization reasons.";
                                    UpdateTextDisplay();
                                    D2Process.Kill();
                                }
                                else
                                {

                                    IntPtr hWnd = D2Process.MainWindowHandle;
                                    if (hWnd != IntPtr.Zero)
                                    {
                                        RECT rect;
                                        if (GetWindowRect(hWnd, out rect))
                                        {
                                            d2window = rect;
                                            if (d2window.Top == 0 & d2window.Right == 0 & d2window.Bottom == 0 && d2window.Left == 0)
                                            {
                                                statusheader = "Launch Conditions:";
                                                statussubtext = "Destiny process found. Awaiting game window.";
                                                UpdateTextDisplay();
                                            }
                                            else
                                            {
                                                if (IntroSection)
                                                {
                                                    statusheader = "Launch Conditions:";
                                                    statussubtext = "Game window found, continuing initialization.";
                                                    UpdateTextDisplay();
                                                    PrepCharMenu();
                                                }
                                                else if(!initializing)
                                                {
                                                    //reset stuff
                                                    if(DateTime.Now > ResetTime)
                                                    {
                                                        //reset has happened. need to restart everything to just scrub the surface clean and reset.

                                                        // Starts a new instance of the program itself
                                                        string appName = Assembly.GetEntryAssembly().GetName().Name;
                                                        string loc = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                                                        if (!File.Exists(loc + "\\reset.ini")) File.Create(loc + "\\reset.ini");

                                                        loc = loc + "\\" + appName + ".exe"; //if you dont do it this way it gives a .dll file instead.
                                                        System.Diagnostics.Process.Start(loc);

                                                        // Closes the current process
                                                        Environment.Exit(0);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            d2window = new RECT();
                                            statusheader = "Launch Conditions:";
                                            statussubtext = "Destiny process found. Awaiting game window.";
                                            UpdateTextDisplay();
                                        }
                                    }
                                }
                            }


                            //close button stuff.

                            if (!closebuttonpressed)
                            {
                                if (Keyboard.IsPressed(0xA5))
                                {
                                    oldstatus = statussubtext;
                                    closebuttonpressed = true;
                                    closetime = DateTime.Now.AddSeconds(5);
                                }
                            }
                            else
                            {
                                if (!Keyboard.IsPressed(0xA5))
                                {
                                    statussubtext = oldstatus;
                                    UpdateTextDisplay();
                                    closebuttonpressed = false;
                                    closetime = DateTime.MaxValue;
                                }
                                else
                                {
                                    statussubtext = "Killing process... " + Math.Ceiling((closetime - DateTime.Now).TotalSeconds);
                                    UpdateTextDisplay();
                                    if (DateTime.Now > closetime) KillProcess();
                                }
                            }

                            Task.Delay(500).Wait();
                        }
                    }).Start();
                }
            }).Start();

            Thread.Sleep(Timeout.Infinite);
        }

        public static void KillProcess()
        {
            D2Process.Kill();
            Environment.Exit(0);
        }

        public static void InitializeCheckpoints()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            checkpoints["CE"] = new Dictionary<string,int>();
            checkpoints["DSC"] = new Dictionary<string, int>();
            checkpoints["DPE"] = new Dictionary<string, int>();
            checkpoints["DP"] = new Dictionary<string, int>();
            checkpoints["SE"] = new Dictionary<string, int>();
            checkpoints["RON"] = new Dictionary<string, int>();
            checkpoints["KF"] = new Dictionary<string, int>();
            checkpoints["VOW"] = new Dictionary<string, int>();
            checkpoints["VOG"] = new Dictionary<string, int>();
            checkpoints["GOS"] = new Dictionary<string, int>();
            checkpoints["LW"] = new Dictionary<string, int>();
            checkpoints["WR"] = new Dictionary<string, int>();
            checkpoints["PIT"] = new Dictionary<string, int>();
            checkpoints["EQ"] = new Dictionary<string, int>();
            checkpoints["SD"] = new Dictionary<string, int>();
            checkpoints["VH"] = new Dictionary<string, int>();
            checkpoints["GOTD"] = new Dictionary<string, int>();
            checkpoints["SOTW"] = new Dictionary<string, int>();
            checkpoints["D"] = new Dictionary<string, int>();
            checkpoints["GOA"] = new Dictionary<string, int>();
            checkpoints["PR"] = new Dictionary<string, int>();
            checkpoints["ST"] = new Dictionary<string, int>();
            checkpoints["CR"] = new Dictionary<string, int>();
            checkpoints["MS"] = new Dictionary<string, int>();
            checkpoints["GAUNTLET"] = new Dictionary<string, int>();

            checkpoints["masterSE"] = new Dictionary<string, int>();
            checkpoints["masterVOG"] = new Dictionary<string, int>();
            checkpoints["masterCE"] = new Dictionary<string, int>();
            checkpoints["masterRON"] = new Dictionary<string, int>();
            checkpoints["masterKF"] = new Dictionary<string, int>();
            checkpoints["masterVOW"] = new Dictionary<string, int>();
            checkpoints["masterVH"] = new Dictionary<string, int>();
            checkpoints["masterSD"] = new Dictionary<string, int>();
            checkpoints["masterWR"] = new Dictionary<string, int>();
            checkpoints["masterGOTD"] = new Dictionary<string, int>();
            checkpoints["masterSOTW"] = new Dictionary<string, int>();
            checkpoints["masterD"] = new Dictionary<string, int>();
            checkpoints["masterGOA"] = new Dictionary<string, int>();

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

                ResetTime = temp; // this is used for detecting when to reset while everything is running.

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
                                    if (checkpoint != "") checkpoints[raidname].Add(cpname,cpindex);
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
                        if (RaidActivityOrder.Count == 11 & DungeonActivityOrder.Count == 11 && PantheonActivityOrder.Count == 3) GottenActivityOrder = true;
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

                    ResetTime = temp;
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
        }

        private static async void InitializeBot()
        {
            client = new(new BotToken(DeveloperToken), new GatewayClientConfiguration()
            {
                Intents = GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.MessageContent,
                Logger = new ConsoleLogger(),
            });

            // Add the handler to handle commands
            client.MessageCreate += HandleMessages;

            //start the client
            await client.StartAsync();
        }

        private static async void AwaitColorChange(double percentageposx, double percentageposy, int count)
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
                Task.Delay(66, OrbitToken).Wait();
                if (OrbitToken.IsCancellationRequested) break;
            }

            runningupdatedetection = false;
            UpdateTextDisplay();

            if (OrbitToken.IsCancellationRequested) return;
            if (!worked)
            {
                ReturnToCharSelect();
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

        public static async void PrepCharMenu()
        {
            IntroSection = false;
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
                awaittext("ExittoDesktop", ConvertAspectRatioCoords(5.15625, 95.972222222), ConvertAspectRatioCoords(14.0625, 98.75));
                Task.Delay(1000).Wait();

                InputSimulator sim = new InputSimulator();
                sim.Keyboard.KeyPress(VirtualKeyCode.ESCAPE);
                Task.Delay(101, OrbitToken).Wait();
                sim.Keyboard.KeyPress(VirtualKeyCode.ESCAPE);
                Task.Delay(101, OrbitToken).Wait();

                statusheader = "Launch Conditions:";
                statussubtext = "Character change menu found, checking if reset has happened since last launch.";
                UpdateTextDisplay();
                //AwaitColorChange(15.82, 29.17, 2); //coords for colors behind the player character.
                Task.Delay(3000).Wait();
                if (!GottenActivityOrder)
                {
                    UpdateStatusBar("Initializing... Grabbing activity order.", UserStatusType.DoNotDisturb);
                    GetToDirectorForActivityCoords();
                    UpdateStatusBar("Init, Checkpoint Cleanup...", UserStatusType.DoNotDisturb);
                    CleanCheckpoints();
                }
                GottenActivityOrder = true;
                oncharselect = true;
                initializing = false;

                UpdateStatusBar("Idling...", UserStatusType.Online);

                statusheader = "Idle...";
                statussubtext = "";
                UpdateTextDisplay();

            }).Start();
        }

        private static async void GetToDirectorForActivityCoords()
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

        private static bool ConnectController()
        {
            try
            {
                _client = new ViGEmClient();
                _controller = _client.CreateXbox360Controller();

                Thread.Sleep(100);

                _controller.Connect();

                _connected = true;
                return true;
            }
            catch (Exception ex)
            {
                _connected = false;
                statusheader = "Cannot find ViGEmBus:";
                statussubtext = "Restarting in 10 seconds... If ViGEmBus isn't installed this will just loop forever."; 
                UpdateTextDisplay();

                Task.Delay(10000);

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

        private static async ValueTask HandleMessages(Message message)
        {
            if (message.ChannelId != ChannelID) return;
            if (message.Content == "") return;
            if (message.Content.First() != '!') return;
            if (!GottenActivityOrder || initializing)
            {
                client.Rest.SendMessageAsync(message.ChannelId, "I'm still getting set up, please be patient with me.\nTry your command again in a couple minutes.");
                return;
            }
            string[] words = message.Content.Split(' ');
            bool done = false;
            if (!verifying) switch (words.First().ToLower())
                {
                    case "!activities":
                        CommandActivities(message);
                        done = true;
                        return;
                    case "!listcommands":
                        done = true;
                        CommandListCommands(message);
                        return;
                    case "!help":
                        done = true;
                        CommandHelp(message);
                        return;
                    case "!holdload":
                        done = true;
                        CommandHoldCheckpoint(message);
                        return;
                    case "!endhold":
                        done = true;
                        CommandEndHoldCheckpoint(message);
                        return;
                    case "!grabcheckpoint":
                        done = true;
                        CommandGrabCheckpoint(message);
                        return;
                    case "!deletecheckpoint":
                        done = true;
                        CommandDeleteCheckpoint(message);
                        return;
                    case "!listcheckpoints":
                        done = true;
                        CommandListCheckpoint(message);
                        return;
                    case "!farmcheckpoint":
                        done = true;
                        CommandFarmCheckpoint(message);
                        return;
                    case "!endfarm":
                        done = true;
                        CommandEndFarm(message);
                        return;
                    case "!forcewipe":
                        done = true;
                        CommandWipe(message);
                        return;
                    case "!transfercheckpoint":
                        done = true;
                        CommandTransferCheckpoint(message);
                        return;
                    case "!forceorbit":
                        done = true;
                        CommandForceOrbit(message);
                        return;
                    case "!flyincheckpointtransfer":
                        done = true;
                        CommandFlyInCheckpointTransfer(message);
                        return;
                    case "!cleancheckpoints":
                        done = true;
                        CommandCleanCheckpoints(message);
                        return;
                }
            if (verifying)
            {
                switch (words.First().ToLower())
                {
                    case "!activities":
                        CommandActivities(message);
                        done = true;
                        return;
                    case "!listcommands":
                        done = true;
                        CommandListCommands(message);
                        return;
                    case "!help":
                        done = true;
                        CommandHelp(message);
                        return;
                    case "!listcheckpoints":
                        done = true;
                        CommandListCheckpoint(message);
                        return;
                    case "!verify":
                        done = true;
                        verifylevel++;
                        return;
                    case "!cancel":
                        verifying = false;
                        done = true;
                        client.Rest.SendMessageAsync(message.ChannelId, "Verification cancelled.");
                        return;
                }
                ;
                if (!done)
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "I'm currently waiting to verify a previous command. please be patient.");
                    done = true;
                }
            }
            if (!done)
            {
                client.Rest.SendMessageAsync(message.ChannelId, "Invalid command, run !listcommands to see all available commands.");
            }
        }

        private static async void CommandCleanCheckpoints(Message message)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                while (afkcycle)
                {
                    if (OrbitToken.IsCancellationRequested) return;
                }

                if (!oncharselect)
                {
                    if (transferingcheckpoint)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm transferring a checkpoint for " + workingdiscordname + "\n" + "If I'm mistaken in this please run either \"!endhold\" or \"!forceorbit\" depending on how mistaken I am.");
                        return;
                    }
                    if (holdingload)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently holding a load for " + workingdiscordname + "\n" + "If I'm mistaken in this please run either \"!endhold\" or \"!forceorbit\" depending on how mistaken I am.");
                        return;
                    }
                    if (checkpointfarmmode)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently helping " + workingdiscordname + " farm " + activityname + " - " + checkpointname + "\nIf I'm mistaken in this please run either \"!endfarm\" or \"!forceorbit\" depending on how mistaken I am.");
                        return;
                    }
                    if (grabbingcheckpoint)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently grabbing a checkpoint from " + workingdiscordname + "\nIf I'm mistaken in this please run \"!forceorbit\" to help me find my bearings.");
                        return;
                    }
                    if (deletingcheckpoint)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently deleting a checkpoint. Please wait a moment.\nIf I'm mistaken in this please run \"!forceorbit\" to help me find my bearings.");
                        return;
                    }
                    client.Rest.SendMessageAsync(message.ChannelId, "I don't believe I'm in orbit right now.\nIf this is a mistake, please run \"!forceorbit\" for me to rectify the situation.");
                    return;
                }

                statusheader = "!CleanCheckpoints command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                verifying = true;
                verifylevel = 0;

                client.Rest.SendMessageAsync(message.ChannelId, "This command takes about 30 minutes to run. I want to make sure we're on the same page here. run \"!verify\" to proceed.");

                DateTime timeout = DateTime.Now.AddMinutes(1);
                while (verifylevel == 0)
                {
                    if (!verifying) return;
                    if (DateTime.Now > timeout)
                    {
                        verifying = false;
                        verifylevel = 0;
                        client.Rest.SendMessageAsync(message.ChannelId, "No valid response given in time. Continuing without returning to orbit.");
                    }
                }
                verifying = false;

                CleanCheckpoints();

            }).Start();
        }

        private static async void CommandFlyInCheckpointTransfer(Message message)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                //make sure reset hasnt happened yet.
                if (checkreset())
                {
                    initializing = true;
                    client.Rest.SendMessageAsync(message.ChannelId, "Looks like I no longer have that checkpoint due to reset and need to clean everything up. Sorry for the inconvenience. I'm gonna be down for the next 30 minutes or so while I work this out.");
                    InitializeCheckpoints();
                    GetToDirectorForActivityCoords();
                    CleanCheckpoints();
                    client.Rest.SendMessageAsync(message.ChannelId, "I'm now done cleaning up post-reset, and I'm funcitonal again now.");

                    initializing = false;
                    return;
                }

                while (afkcycle)
                {
                    if (OrbitToken.IsCancellationRequested) return;
                }

                if (!oncharselect)
                {
                    if (transferingcheckpoint)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm transferring a checkpoint for " + workingdiscordname + "\n" + "If I'm mistaken in this please run either \"!endhold\" or \"!forceorbit\" depending on how mistaken I am.");
                        return;
                    }
                    if (holdingload)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently holding a load for " + workingdiscordname + "\n" + "If I'm mistaken in this please run either \"!endhold\" or \"!forceorbit\" depending on how mistaken I am.");
                        return;
                    }
                    if (checkpointfarmmode)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently helping " + workingdiscordname + " farm " + activityname + " - " + checkpointname + "\nIf I'm mistaken in this please run either \"!endfarm\" or \"!forceorbit\" depending on how mistaken I am.");
                        return;
                    }
                    if (grabbingcheckpoint)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently grabbing a checkpoint from " + workingdiscordname + "\nIf I'm mistaken in this please run \"!forceorbit\" to help me find my bearings.");
                        return;
                    }
                    if (deletingcheckpoint)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently deleting a checkpoint. Please wait a moment.\nIf I'm mistaken in this please run \"!forceorbit\" to help me find my bearings.");
                        return;
                    }
                    client.Rest.SendMessageAsync(message.ChannelId, "I don't believe I'm in orbit right now.\nIf this is a mistake, please run \"!forceorbit\" for me to rectify the situation.");
                    return;
                }

                statusheader = "!FlyInCheckpointTransfer command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                UpdateStatusBar("!FlyInCheckpointTransfer... Making sure the command is viable.", UserStatusType.Idle);

                grabbingcheckpoint = true;

                //figure out what character to grab the checkpoint on. if my checkpoints are full, bail.
                string[] messagechunks = message.Content.Split(" ");
                //!GrabCheckpoint [activity shorthand (!activities)] [(optional)master] [single word name for the checkpoint of your choosing.  a-z, 1-9 only] BungieUsername#0000
                if (messagechunks.Length < 4)
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "Improper use of the \"!flyincheckpointtransfer\" command.\nTry \"!help flyincheckpointtransfer\" to learn how to use it.");
                    grabbingcheckpoint = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }

                string[] nameID = messagechunks.Last().Split("#");
                if (nameID.Length == 1 || nameID.Last().Split('#').Last().Length != 4)
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "For the \"!flyincheckpointtransfer\" command to work, I need the 4 number hashtag after your guardians name.\nTry \"!help flyincheckpointtransfer\" to learn how to use it.");
                    grabbingcheckpoint = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }

                string activity = messagechunks[1].ToUpper();
                string activitykey = activity;

                string[] keys = checkpoints.Keys.ToArray();
                if (!keys.Contains(activity))
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "I appear to not know what activity " + activity + " is.\nTry \"!help flyincheckpointtransfer\" to learn how to use this command, or \"!activities\" to see what activities are available.");
                    grabbingcheckpoint = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }
                bool master = false;
                string ckpointname = messagechunks[2];

                workingdiscordname = message.Author.Username;
                if (message.Author.GlobalName != null) workingdiscordname = message.Author.GlobalName;
                int namestartindex = 3;

                if (messagechunks[2].ToLower() == "master")
                {
                    namestartindex = 4;
                    ckpointname = messagechunks[3];
                    master = true;
                    if (!keys.Contains("master" + activity))
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "That activity doesn't appear to have a master mode.\nTry \"!help flyincheckpointtransfer\" to learn how to use this command.");
                        grabbingcheckpoint = false;

                        statusheader = "Idle...";
                        statussubtext = "";
                        UpdateTextDisplay();

                        UpdateStatusBar("Idle...", UserStatusType.Online);
                        return;
                    }
                    else
                    {
                        activitykey = "master" + activity;
                    }
                }

                workingusername = "";
                for (int i = namestartindex; i < messagechunks.Length; i++)
                {
                    if (workingusername == "") workingusername = workingusername + messagechunks[i];
                    else workingusername = workingusername + " " + messagechunks[i];
                }

                Regex rgx = new Regex("[^a-zA-Z0-9 -]");
                ckpointname = rgx.Replace(ckpointname, "");

                int charslot = 0;
                if (checkpoints[activitykey].Keys.Contains(ckpointname))
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "I already have a checkpoint with the name " + ckpointname + " in my save data.\nTry \"!help flyincheckpointtransfer\" to learn how to use this command.");
                    grabbingcheckpoint = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }
                if (checkpoints[activitykey].Keys.Count == 3)
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "I already have 3 checkpoints for that activity in my record.\nTry \"!help deletecheckpoint\" to learn how to delete a checkpoint so that you may overwrite it, and \"!listcheckpoints\" to see what checkpoints I have.");
                    grabbingcheckpoint = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }
                else
                {
                    charslot = 1;
                    //charslot = checkpoints[activitykey].Count + 1;
                    foreach (string tk in checkpoints[activitykey].Keys)
                    {
                        if (checkpoints[activitykey][tk] == 1) charslot = 2;
                        if (checkpoints[activitykey][tk] == 2) charslot = 3;
                    }
                }

                if (ckpointname == "")
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "Somehow you've managed to give me a checkpoint name that when filtered for only numbers and letters is an empty string.\nTry \"!help flyincheckpointtransfer\" to learn how to use this command.");
                    grabbingcheckpoint = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }

                //activity for the activity name (shorthand + master)
                //charslot for which character the checkpoint is stored on.
                //master (bool) to know if its master mode or not

                statusheader = "!FlyInCheckpointTransfer command:";
                statussubtext = "Attempting to join...";
                UpdateTextDisplay();

                UpdateStatusBar("!FlyInCheckpointTransfer: Attempting to join...", UserStatusType.Idle);

                client.Rest.SendMessageAsync(message.ChannelId, "Checking to see if I can join...");
                SelectChar(charslot);

                bool worked = JoinFireteamInOrbit("/join " + workingusername);
                if (!worked)
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "Looks like your fireteam is currently unavailable. Returning to idling.");
                    grabbingcheckpoint = false;
                    ReturnToCharSelect();

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                }
                else
                {
                    //make sure its not an error code please TODO - Not sure how to do this honestly.

                    UpdateStatusBar("!FlyInCheckpointTransfer: Making sure the checkpoint saved correctly.", UserStatusType.Idle);

                    statusheader = "!FlyInCheckpointTransfer command:";
                    statussubtext = "Boots on ground. Returning to orbit.";
                    UpdateTextDisplay();

                    //return to char select, then see if the checkpoint saved.
                    ReturnToCharSelect();
                    SelectChar(charslot);
                    SelectDirector();
                    SelectPortal();
                    Task.Delay(400, OrbitToken).Wait();

                    SelectActivity(activity);
                    if (master) SelectMaster();

                    Task.Delay(2000, OrbitToken).Wait();

                    //highlight over the play button just to have a consistent location for the checkpoint on screen.
                    SetCursorPos(ConvertAspectRatioCoords(75.117, 83.75).X, ConvertAspectRatioCoords(75.117, 83.75).Y);

                    Task.Delay(1000, OrbitToken).Wait();

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
                        checkpoints[activitykey].Add(ckpointname, charslot);
                        savecheckpoints();
                        client.Rest.SendMessageAsync(message.ChannelId, "Checkpoint grabbed successfully. Returning to orbit.");
                    }
                    else
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "It looks like the checkpoint failed to grab. Please try the command again or use a different method of getting a checkpoint.");
                    }

                    grabbingcheckpoint = false;
                    bootsonground = false;
                    ReturnToCharSelect();

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                }
            }).Start();

        }

        private static async void CommandHelp(Message message)
        {
            //check if the command is run on its own with no arguments. if it is, run ListCommands.
            //else, check if the command exists. if the command exists, give its specific description.
            string[] parse = message.Content.Split(' ');
            if (parse.Length == 1)
            {
                CommandListCommands(message);
                return;
            }
            else
            {
                bool done = false;
                switch (parse[1].ToLower().Replace("!", ""))
                {
                    case "listcommands":
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        client.Rest.SendMessageAsync(message.ChannelId,
                            "### !ListCommands:\n" +
                            " - I'll list all available commands and their definitions.\n" +
                            " - Usage: !ListCommands\n");
                        done = true;
                        return;

                    case "activities":
                        client.Rest.SendMessageAsync(message.ChannelId,
                        "### !Activities\n" +
                        " - I'll list all available activities.\n" +
                        " - usage: !Activities\n");
                        done = true;
                        return;
                    case "holdload":
                        client.Rest.SendMessageAsync(message.ChannelId,
                        "### !HoldLoad: \n" +
                        " - I'll hold a load by joining on the person until the !EndHold command is run.\n" +
                        " - usage: !HoldLoad BungieUsername#0000\n");
                        done = true;
                        return;
                    case "endhold":
                        client.Rest.SendMessageAsync(message.ChannelId,
                            "### !EndHold: \n" +
                            " - First I will confirm that you do want to end the hold, with !verify.\n" +
                            " - After that, I'll return to orbit and enter idle mode.\n" +
                            " - usage: !EndHold \n");
                        done = true;
                        return;
                    case "grabcheckpoint":
                        client.Rest.SendMessageAsync(message.ChannelId,
                            "### !GrabCheckpoint: \n" +
                            " - Has me join on the username given with the command, and then wait for a wipe so that I have the checkpoint, at which point I'll then return to orbit and idle.\n" +
                            " - usage: !GrabCheckpoint [activity shorthand (!activities)] [(optional)master] [single word name for the checkpoint of your choosing.  a-z, 1-9 only] BungieUsername#0000 \n" +
                            " - example: !GrabCheckpoint WR ogre ItAvvy#7006\n" +
                            " - If I already have a checkpoint in that activity on all 3 characters I will list them and ask you to delete one using !deletecheckpoint.");
                        done = true;
                        return;
                    case "deletecheckpoint":
                        client.Rest.SendMessageAsync(message.ChannelId,
                            "### !DeleteCheckpoint:\n" +
                            " - I'll delete a checkpoint from a given activity with a given name so that a new checkpoint may be gotten on that character.\n" +
                            " - usage: !DeleteCheckpoint [activity shorthand (!activities)] [(optional)master] [single word name of the checkpoint. a-z, 0-9 only] \n" +
                            " - example: !DeleteCheckpoint WR master ogre");
                        done = true;
                        return;
                    case "listcheckpoints":
                        client.Rest.SendMessageAsync(message.ChannelId,
                            "### !ListCheckpoint: \n" +
                            " - Lists out all checkpoints on a given activity, and specifies master in cases where its applicable.\n" +
                            " - usage: !ListCheckpoint [activity shorthand (!activities)]\n" +
                            " - use !ListCheckpoint All - to see all available checkpoints across all activities");
                        done = true;
                        return;
                    case "farmcheckpoint":
                        client.Rest.SendMessageAsync(message.ChannelId,
                            "### !FarmCheckpoint: \n" +
                            " - I'll load the character the given checkpoint is on, and I'll wait in orbit for you to join. The moment you join I'll launch the activity, transferring the checkpoint on load-in. Then, I will return to orbit to wait to launch again. Use !EndFarm to end the farm. If you specify feats with the optional modifier, I will try to launch the checkpoint with those feats if applicable.\n" +
                            " - Viable Feats: Token, Phase, Battalions, Challenges, and Cutthroat. \n" +
                            " - usage: !FarmCheckpoint [activity shorthand (!activities)] [(optional)master] [(optional)feats:feat1name,feat2name,etc...] [single word name for the checkpoint of your choosing. a-z, 0-9 only]  BungieUsername#0000 \n" +
                            " - example: !FarmCheckpoint EQ feats:tokenlimit,phaselimit shockyhands ItAvvy#7006");
                        done = true;
                        return;
                    case "endfarm":
                        client.Rest.SendMessageAsync(message.ChannelId,
                            "### !EndFarm: \n" +
                            " - I'll stop farming the given activity and shift into idle mode. I will then ask for you to run !verify to verify that you do in fact want to end the farm.\n" +
                            " - Usage: !EndFarm");
                        done = true;
                        return;
                    case "forcewipe":
                        client.Rest.SendMessageAsync(message.ChannelId,
                            "### !ForceWipe: \n" +
                            " - If applicable, I'll fire a rocket at the ground to force a wipe.\n" +
                            " - usage: !ForceWipe");
                        done = true;
                        return;
                    case "help":
                        client.Rest.SendMessageAsync(message.ChannelId,
                            "### !Help: \n" +
                            " - Why are you asking for help with the \"help\" command???\n" +
                            " - usage: !Help [command name]");
                        done = true;
                        return;
                    case "forceorbit":
                        client.Rest.SendMessageAsync(message.ChannelId,
                            "### !ForceOrbit\n" +
                            " - Has me attempt to change characters thru the settings menu, to rescue myself from a softlock of some kind. May not always work. At which point I will forget everything I was doing, and will need to be set back up for farms and stuff.\n" +
                            " - I will ask for confirmation twice before doing this.\n" +
                            " - Usage: !ForceOrbit");
                        done = true;
                        return;
                    case "transfercheckpoint":
                        client.Rest.SendMessageAsync(message.ChannelId,
                            "### !TransferCheckpoint \n" +
                            " - Has me load into a checkpoint only a single time after someone joins my lobby, so that I may transfer the checkpoint to them, and then I'll return to idle.\n" +
                            " - Usage: !TransferCheckpoint [activity shorthand(!activities)] [(optional)master] [single word name given to the checkpoint.  a-z, 0-9 only] BungieUsername#0000");
                        done = true;
                        return;
                    case "flyincheckpointtransfer":
                        client.Rest.SendMessageAsync(message.ChannelId,
                            "### !FlyInCheckpointTransfer\n" +
                            " - Transfers a checkpoint from you, to me, the hard way without using a darkness zone.\n" +
                            " - Warning, this requires a bit of cooperation, and will only work if your fireteam is set to open.\n" +
                            " - First, navigate on your director to the activity that has the checkpoint you want to transfer on it, and wait in orbit. Then run this command.\n" +
                            " - After you run the command I will attempt to join on you.\n" +
                            " - Then, I'll ask you to launch the activity, open your inventory, navigate to \"change character\" in your settings, click it, and then have you wait to confirm.\n" +
                            " - Once my screen goes black, I'll send a chat message telling you to hit confirm to change characters.\n" +
                            " - Once I'm boots on the ground I will return to orbit, verify that I do have the checkpoint, and echo the result here.\n" +
                            " - Usage: !TransferCheckpoint [activity shorthand (!activities)] [(optional)master] [single word name of the checkpoint.  a-z, 1-9 only] BungieUsername#0000");
                        done = true;
                        return;
                    case "cleancheckpoints":
                        client.Rest.SendMessageAsync(message.ChannelId,
                            "### !CleanCheckpoints\n" +
                            " - I will go thru, activity by activity, both normal and master and delete any erronious checkpoints I may have that I don't have record of.\n" +
                            " - This does require you to verify that you want to do it beforehand, as it takes about 30 minutes to go thru everything.\n" +
                            " - Usage: !CleanCheckpoints");
                        done = true;
                        return;
                }
                if (!done)
                {
                    client.Rest.SendMessageAsync(message.ChannelId,
                        "The command you asked about doesn't seem to exist.\nPlease verify your spelling and try again or use the !ListCommands command.");
                }
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        } 

        private static async void CommandForceOrbit(Message message)
        {
            //start a new thread so i can get more commands in the meantime. it doesnt matter here but it will in other places.

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                //verify.
                verifying = true;
                verifylevel = 0;

                string oldstatus = statusheader;
                string oldsubtext = statussubtext;

                statusheader = "Processing !ForceOrbit command:";
                statussubtext = "Awaiting first verification.";
                UpdateTextDisplay();

                UpdateStatusBar("!ForceOrbit: Verification step 1/2", UserStatusType.Idle);

                client.Rest.SendMessageAsync(message.ChannelId, "I want to make sure we're on the same page. This will make me forget what I was doing and send me back to character select.\nSend \"!verify\" to confirm, or \"!cancel\" to cancel.\nIf no response is given in 60 seconds I will cancel on my own.");

                DateTime timeout = DateTime.Now.AddMinutes(1);
                while (verifylevel == 0)
                {
                    if (!verifying) return;
                    if (DateTime.Now > timeout)
                    {
                        verifying = false;
                        verifylevel = 0;
                        client.Rest.SendMessageAsync(message.ChannelId, "No valid response given in time. Continuing without returning to orbit.");

                        statusheader = oldstatus;
                        statussubtext = oldsubtext;
                        UpdateTextDisplay();
                        UpdateStatusBar("Idle...", UserStatusType.Online);
                    }
                }
                //verify again.

                statusheader = "Processing !ForceOrbit command:";
                statussubtext = "Awaiting second verification.";
                UpdateTextDisplay();

                UpdateStatusBar("!ForceOrbit: Verification step 2/2", UserStatusType.Idle);

                client.Rest.SendMessageAsync(message.ChannelId, "I'm double checking. \"!verify\" to verify again, \"!cancel\" to cancel.");
                timeout = DateTime.Now.AddMinutes(1);
                while (verifylevel == 1)
                {
                    if (!verifying) return;
                    if (DateTime.Now > timeout)
                    {
                        verifying = false;
                        verifylevel = 0;
                        client.Rest.SendMessageAsync(message.ChannelId, "No valid response given in time. Continuing without returning to orbit.");

                        statusheader = oldstatus;
                        statussubtext = oldsubtext;
                        UpdateTextDisplay();
                        UpdateStatusBar("Idle...", UserStatusType.Online);
                    }
                }

                statusheader = "Processing !ForceOrbit command:";
                statussubtext = "Both confirmations detected. Returning to orbit and canceling all other tasks.";
                UpdateTextDisplay();

                UpdateStatusBar("!ForceOrbit: Killing processes...", UserStatusType.Idle);

                //change characters. 
                client.Rest.SendMessageAsync(message.ChannelId, "Returning to orbit and forgetting what I was doing...");
                //reset all running variables. forget about farms. forget about holding loads. etc... fresh start.

                holdingload = false;
                checkpointfarmmode = false;
                afkcycle = false;
                bootsonground = false;
                grabbingcheckpoint = false;
                verifylevel = 0;
                verifying = false;
                transferingcheckpoint = false;

                OrbitTokenSource.Cancel();

                Task.Delay(1000).Wait();

                OrbitTokenSource.TryReset();

                UpdateStatusBar("!ForceOrbit: Returning to orbit as safely as possible...", UserStatusType.Idle);
                ReturnToCharSelect();

                UpdateStatusBar("Idle...", UserStatusType.Online);
            }).Start();
        }

        private static async void CommandTransferCheckpoint(Message message) 
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                //make sure reset hasnt happened yet.
                if (checkreset())
                {
                    initializing = true;
                    client.Rest.SendMessageAsync(message.ChannelId, "Looks like I no longer have that checkpoint due to reset and need to clean everything up. Sorry for the inconvenience. I'm gonna be down for the next 30 minutes or so while I work this out.");
                    InitializeCheckpoints();
                    GetToDirectorForActivityCoords();
                    CleanCheckpoints();
                    client.Rest.SendMessageAsync(message.ChannelId, "I'm now done cleaning up post-reset, and I'm funcitonal again now.");

                    initializing = false;
                    return;
                }

                while (afkcycle)
                {
                    if (OrbitToken.IsCancellationRequested) return;
                }

                if (!oncharselect)
                {
                    if (transferingcheckpoint)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm transferring a checkpoint for " + workingdiscordname + "\n" + "If I'm mistaken in this please run either \"!endhold\" or \"!forceorbit\" depending on how mistaken I am.");
                        return;
                    }
                    if (holdingload)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently holding a load for " + workingdiscordname + "\n" + "If I'm mistaken in this please run either \"!endhold\" or \"!forceorbit\" depending on how mistaken I am.");
                        return;
                    }
                    if (checkpointfarmmode)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently helping " + workingdiscordname + " farm " + activityname + " - " + checkpointname + "\nIf I'm mistaken in this please run either \"!endfarm\" or \"!forceorbit\" depending on how mistaken I am.");
                        return;
                    }
                    if (grabbingcheckpoint)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently grabbing a checkpoint from " + workingdiscordname + "\nIf I'm mistaken in this please run \"!forceorbit\" to help me find my bearings.");
                        return;
                    }
                    if (deletingcheckpoint)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently deleting a checkpoint. Please wait a moment.\nIf I'm mistaken in this please run \"!forceorbit\" to help me find my bearings.");
                        return;
                    }
                    client.Rest.SendMessageAsync(message.ChannelId, "I don't believe I'm in orbit right now.\nIf this is a mistake, please run \"!forceorbit\" for me to rectify the situation.");
                    return;
                }

                statusheader = "!TransferCheckpoint command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                UpdateStatusBar("!TransferCheckpoint... Making sure the command is viable.", UserStatusType.Idle);

                transferingcheckpoint = true;
                client.Rest.SendMessageAsync(message.ChannelId, "Making sure I have the checkpoint and everything is correct...");

                //parse command text
                //!TransferCheckpoint [activity shorthand(!activities)] [(optional)master] [single word name given to the checkpoint. a-z, 1-9 only] BungieUsername#0000

                string[] messagechunks = message.Content.Split(" ");

                if (messagechunks.Length < 4)
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "Improper use of the \"!transfercheckpoint\" command.\nTry \"!help transfercheckpoint\" to learn how to use it.");
                    transferingcheckpoint = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }

                string[] nameID = messagechunks.Last().Split("#");
                if (nameID.Length == 1 || nameID.Last().Split('#').Last().Length != 4)
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "For the \"!transfercheckpoint\" command to work, I need the 4 number hashtag after your guardians name.\nTry \"!help transfercheckpoint\" to learn how to use it.");
                    transferingcheckpoint = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }

                string activity = messagechunks[1].ToUpper();
                string activitykey = activity;

                string[] keys = checkpoints.Keys.ToArray();
                if (!keys.Contains(activity))
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "I appear to not know what activity " + activity + " is.\nTry \"!help transfercheckpoint\" to learn how to use this command, or \"!activities\" to see what activities are available.");
                    transferingcheckpoint = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }
                bool master = false;
                string ckpointname = messagechunks[2];

                workingdiscordname = message.Author.Username;
                if (message.Author.GlobalName != null) workingdiscordname = message.Author.GlobalName;
                int namestartindex = 3;

                if (messagechunks[2].ToLower() == "master")
                {
                    namestartindex = 4;
                    checkpointname = messagechunks[3];
                    master = true;
                    if (!keys.Contains("master" + activity))
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "That activity doesn't appear to have a master mode.\nTry \"!help transfercheckpoint\" to learn how to use this command.");
                        transferingcheckpoint = false;

                        statusheader = "Idle...";
                        statussubtext = "";
                        UpdateTextDisplay();

                        UpdateStatusBar("Idle...", UserStatusType.Online);
                        return;
                    }
                    else
                    {
                        activitykey = "master" + activity;
                        ckpointname = messagechunks[3];
                    }
                }
                workingusername = "";
                for (int i = namestartindex; i < messagechunks.Length; i++)
                {
                    if (workingusername == "") workingusername = workingusername + messagechunks[i];
                    else workingusername = workingusername + " " + messagechunks[i];
                }

                Regex rgx = new Regex("[^a-zA-Z0-9 -]");
                ckpointname = rgx.Replace(ckpointname, "");

                int charslot = 0;
                if (checkpoints[activitykey].Keys.Contains(ckpointname))
                {
                    charslot = checkpoints[activitykey][ckpointname];
                }
                else { 
                    client.Rest.SendMessageAsync(message.ChannelId, "I don't currently have a checkpoint with the name " + ckpointname + " in my save data.\nTry \"!help transfercheckpoint\" to learn how to use this command, or \"!listcheckpoints\" to see what checkpoints I have.");
                    transferingcheckpoint = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }

                SelectChar(charslot);
                SelectDirector();
                SelectPortal();
                SelectActivity(activity);
                if (master) SelectMaster();

                client.Rest.SendMessageAsync(message.ChannelId, "Sending an invite to " + workingusername + ". If I don't see someone join in the next 5 minutes I will return to idle mode.");
                InvitePlayer("/invite " + workingusername);

                statussubtext = "Invite sent. Waiting for player to join.";
                UpdateTextDisplay();

                Point pointcheck = ConvertAspectRatioCoords(95.859, 83.75);
                Point pointclick = ConvertAspectRatioCoords(75.117, 83.75);

                SetCursorPos(pointclick.X, pointclick.Y);

                Task.Delay(1000, OrbitToken).Wait();

                Color spotcolor = GetColorAt(pointcheck);

                bool change = false;

                DateTime time = DateTime.Now.AddMinutes(5);
                statussubtext = "Comparing red values to see launch button go red.";
                UpdateTextDisplay();

                UpdateStatusBar("!TransferCheckpoint... Waiting for " + workingusername + " to join.", UserStatusType.Idle);

                while (!change)
                {
                    Color spotcolor2 = GetColorAt(pointcheck);

                    if (Math.Abs(spotcolor.G - spotcolor2.G) > 60)
                    {
                        if (spotcolor2.G != 0) change = true;
                    }

                    if (DateTime.Now > time || OrbitToken.IsCancellationRequested)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "Nobody joined. Returning to orbit.");

                        transferingcheckpoint = false;
                        ReturnToCharSelect();

                        UpdateStatusBar("Idle...", UserStatusType.Online);
                        return;
                    }
                    Task.Delay(101, OrbitToken).Wait();
                }

                statussubtext = "Button went red. Waiting for it to go back.";
                UpdateTextDisplay();

                UpdateStatusBar("!TransferCheckpoint... Detected join, Launching momentarily.", UserStatusType.Idle);

                AwaitColorChange(95.859, 83.75,1);

                statussubtext = "Join detected. Launching activity.";
                UpdateTextDisplay();

                client.Rest.SendMessageAsync(message.ChannelId, "Launching activity, and then returning to orbit. Good luck on your run(s).");

                Task.Delay(2000,OrbitToken).Wait();
                change = false;

                while (!change)
                {
                    Color spotcolor2 = GetColorAt(pointcheck);

                    if (Math.Abs(spotcolor.G - spotcolor2.G) > 80)
                    {
                        if (spotcolor2.G != 0) change = true;
                    }

                    SendClick(pointclick);

                    if (OrbitToken.IsCancellationRequested)
                    {
                        transferingcheckpoint = false;
                        ReturnToCharSelect();
                        return;
                    }
                    Task.Delay(250, OrbitToken).Wait();
                }

                statussubtext = "Awaiting first black screen.";
                UpdateTextDisplay();

                Task.Delay(2000, OrbitToken).Wait();

                DateTime bailout = DateTime.Now.AddSeconds(30);

                while (GetColorAt(ConvertAspectRatioCoords(50, 50)) != black)
                {
                    if (OrbitToken.IsCancellationRequested)
                    {
                        transferingcheckpoint = false;
                        ReturnToCharSelect();

                        UpdateStatusBar("Idle...", UserStatusType.Online);
                        return;
                    }

                    if (DateTime.Now > bailout) break;
                }

                transferingcheckpoint = false;
                ReturnToCharSelectFast();

                UpdateStatusBar("Idle...", UserStatusType.Online);

            }).Start();
        }

        private static async void CommandWipe(Message message) 
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                //check if im currently grabbing a checkpoint.
                if (grabbingcheckpoint)
                {
                    statussubtext = "Queued wipe command...";
                    UpdateTextDisplay();

                    client.Rest.SendMessageAsync(message.ChannelId, "Making sure im boots on the ground, so that I can wipe.");
                    //check if im boots on the ground. idk how yet. maybe trying to swap menus with dpad and seeing what happens?
                    while (!bootsonground)
                    {
                        //add in a contengency to bail out if someone forces the bot to orbit.
                        if (OrbitToken.IsCancellationRequested) break;
                    }
                    if (OrbitToken.IsCancellationRequested) return;
                    //lmao explode.

                    statussubtext = "Wipe Command: Detonating :3";
                    UpdateTextDisplay();

                    _controller.SetAxisValue(Xbox360Axis.RightThumbY, STICK_BACK);
                    _controller.SetButtonState(Xbox360Button.Y, true);
                    Task.Delay(2000, OrbitToken).Wait();
                    if (OrbitToken.IsCancellationRequested) return;
                    _controller.SetButtonState(Xbox360Button.Y, false);
                    Task.Delay(2000, OrbitToken).Wait();
                    if (OrbitToken.IsCancellationRequested) return;
                    _controller.SetAxisValue(Xbox360Axis.RightThumbY, STICK_CENTER);
                    _controller.SetSliderValue(Xbox360Slider.RightTrigger, TRIGGER_PULLED);
                    Task.Delay(1000, OrbitToken).Wait();
                    if (OrbitToken.IsCancellationRequested) return;
                    _controller.SetSliderValue(Xbox360Slider.RightTrigger, TRIGGER_RELEASED);
                    client.Rest.SendMessageAsync(message.ChannelId, ":boom::white_check_mark: :3");

                    statussubtext = "Wipe Command: I hope this worked.";
                    UpdateTextDisplay();
                }
                else
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "I'm not currently grabbing a checkpoint. That's so rude to ask me to do right now.");
                }
            }).Start();
        }

        private static async void CommandEndFarm(Message message)
        {
            //check if a farm is even running.
            //ask for confirmation.
            //if no confirm in 60 seconds, forget this ever happened.
            //endfarm.
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                if(!checkpointfarmmode)
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "I don't believe I'm currently running a farm. If I'm mistaken please run \"!Forceorbit\" to help me find where I am.");
                    return;
                }

                client.Rest.SendMessageAsync(message.ChannelId, "I want to make sure we're on the same page. This will make me forget what I was doing and send me back to character select.\nSend \"!verify\" to confirm, or \"!cancel\" to cancel.\nIf no response is given in 60 seconds I will cancel on my own.");

                verifying = true;

                DateTime timeout = DateTime.Now.AddMinutes(1);
                while (verifylevel == 0)
                {
                    if (!verifying) return;
                    if (DateTime.Now > timeout)
                    {
                        verifying = false;
                        verifylevel = 0;
                        client.Rest.SendMessageAsync(message.ChannelId, "No valid response given in time. Continuing without returning to orbit.");
                    }
                }

                verifying = false;
                checkpointfarmmode = false;
                ReturnToCharSelect();

                UpdateStatusBar("Idle...", UserStatusType.Online);

            }).Start();
        } 

        private static async void CommandFarmCheckpoint(Message message) //TODO test proph
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                //make sure reset hasnt happened yet.
                if (checkreset())
                {
                    initializing = true;
                    client.Rest.SendMessageAsync(message.ChannelId, "Looks like I no longer have that checkpoint due to reset and need to clean everything up. Sorry for the inconvenience. I'm gonna be down for the next 30 minutes or so while I work this out.");
                    InitializeCheckpoints();
                    GetToDirectorForActivityCoords();
                    CleanCheckpoints();
                    client.Rest.SendMessageAsync(message.ChannelId, "I'm now done cleaning up post-reset, and I'm funcitonal again now.");

                    initializing = false;
                    return;
                }

                while (afkcycle)
                {
                    if (OrbitToken.IsCancellationRequested) return;
                }

                if (!oncharselect)
                {
                    if (transferingcheckpoint)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm transferring a checkpoint for " + workingdiscordname + "\n" + "If I'm mistaken in this please run either \"!endhold\" or \"!forceorbit\" depending on how mistaken I am.");
                        return;
                    }
                    if (holdingload)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently holding a load for " + workingdiscordname + "\n" + "If I'm mistaken in this please run either \"!endhold\" or \"!forceorbit\" depending on how mistaken I am.");
                        return;
                    }
                    if (checkpointfarmmode)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently helping " + workingdiscordname + " farm " + activityname + " - " + checkpointname + "\nIf I'm mistaken in this please run either \"!endfarm\" or \"!forceorbit\" depending on how mistaken I am.");
                        return;
                    }
                    if (grabbingcheckpoint)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently grabbing a checkpoint from " + workingdiscordname + "\nIf I'm mistaken in this please run \"!forceorbit\" to help me find my bearings.");
                        return;
                    }
                    if (deletingcheckpoint)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently deleting a checkpoint. Please wait a moment.\nIf I'm mistaken in this please run \"!forceorbit\" to help me find my bearings.");
                        return;
                    }
                    client.Rest.SendMessageAsync(message.ChannelId, "I don't believe I'm in orbit right now.\nIf this is a mistake, please run \"!forceorbit\" for me to rectify the situation.");
                    return;
                }

                statusheader = "!FarmCheckpoint command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                UpdateStatusBar("!FarmCheckpoint... Validating command.", UserStatusType.Idle);

                checkpointfarmmode = true;

                //figure out what character to grab the checkpoint on. if my checkpoints are full, bail.
                string[] messagechunks = message.Content.Split(" ");

                //!FarmCheckpoint [activity shorthand (!activities)] [(optional)master] [(optional)feats:feat1name,feat2name,etc...] [single word name for the checkpoint of your choosing. a-z, 1-9 only] BungieUsername#0000
                if (messagechunks.Length < 4)
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "Improper use of the \"!FarmCheckpoint\" command.\nTry \"!help FarmCheckpoint\" to learn how to use it.");
                    checkpointfarmmode = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();
                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }

                string[] nameID = messagechunks.Last().Split("#");
                if (nameID.Length == 1 || nameID.Last().Split('#').Last().Length != 4)
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "For the \"!FarmCheckpoint\" command to work, I need the 4 number hashtag after your guardians name.\nTry \"!help FarmCheckpoint\" to learn how to use it.");
                    checkpointfarmmode = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();
                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }

                string activity = messagechunks[1].ToUpper();
                string activitykey = activity;

                string[] keys = checkpoints.Keys.ToArray();
                if (!keys.Contains(activity))
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "I appear to not know what activity " + activity + " is.\nTry \"!help FarmCheckpoint\" to learn how to use this command, or \"!activities\" to see what activities are available.");
                    checkpointfarmmode = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();
                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }
                bool master = false;
                string ckpointname = messagechunks[2];

                workingdiscordname = message.Author.Username;
                if (message.Author.GlobalName != null) workingdiscordname = message.Author.GlobalName;
                int namestartindex = 3;

                if (messagechunks[2].ToLower() == "master")
                {
                    namestartindex = 4;
                    ckpointname = messagechunks[3];
                    master = true;
                    if (!keys.Contains("master" + activity))
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "That activity doesn't appear to have a master mode.\nTry \"!help FarmCheckpoint\" to learn how to use this command.");
                        checkpointfarmmode = false;

                        statusheader = "Idle...";
                        statussubtext = "";
                        UpdateTextDisplay();
                        UpdateStatusBar("Idle...", UserStatusType.Online);
                        return;
                    }
                    else
                    {
                        activitykey = "master" + activity;
                    }
                }

                string[] possiblefeats = {"token","phase","battalions","challenges","cutthroat"};

                List<string> featlist = new List<string>();
                bool feats = false;
                if (!master)
                {
                    if (messagechunks[2].ToLower().Split(":")[0] == "feats" & messagechunks[2].ToLower().Split(":").Length > 1)
                    {
                        if(activitykey != "DP" & activitykey != "EDP" & activitykey != "EQ")
                        {
                            client.Rest.SendMessageAsync(message.ChannelId, "The activity you've selected does not support feats. Possible activities with feats are: \nDP, EDP, and EQ.");
                            checkpointfarmmode = false;

                            statusheader = "Idle...";
                            statussubtext = "";
                            UpdateTextDisplay();
                            UpdateStatusBar("Idle...", UserStatusType.Online);
                            return;
                        }
                        feats = true;
                        namestartindex = 4;
                        ckpointname = messagechunks[3];


                        foreach (string feat in messagechunks[2].Split(":")[1].Split(","))
                        {
                            if (!possiblefeats.Contains(feat.ToLower()))
                            {
                                client.Rest.SendMessageAsync(message.ChannelId, "One or more of the feats you requested aren't in my system. Legal feats include: \nToken, Phase, Battalions, Challenges, and Cutthroat.");
                                checkpointfarmmode = false;

                                statusheader = "Idle...";
                                statussubtext = "";
                                UpdateTextDisplay();
                                UpdateStatusBar("Idle...", UserStatusType.Online);
                                return;
                            }
                            else
                            {
                                featlist.Add(feat.ToLower());
                            }
                        }
                    }
                }

                workingusername = "";
                for (int i = namestartindex; i < messagechunks.Length; i++)
                {
                    if (workingusername == "") workingusername = workingusername + messagechunks[i];
                    else workingusername = workingusername + " " + messagechunks[i];
                }

                Regex rgx = new Regex("[^a-zA-Z0-9 -]");
                ckpointname = rgx.Replace(ckpointname, "");

                int charslot = 0;
                if (checkpoints[activitykey].Keys.Contains(ckpointname))
                {
                    charslot = checkpoints[activitykey][ckpointname];
                }
                else
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "I don't currently have a checkpoint with the name " + ckpointname + " in my save data.\nTry \"!help transfercheckpoint\" to learn how to use this command, or \"!listcheckpoints\" to see what checkpoints I have.");
                    checkpointfarmmode = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();
                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }

                while (checkpointfarmmode)
                {
                    if (!checkpointfarmmode) break;
                    SelectChar(charslot);
                    if (!checkpointfarmmode) break;
                    SelectDirector();
                    if (!checkpointfarmmode) break;
                    SelectPortal();
                    if (!checkpointfarmmode) break;
                    SelectActivity(activity);
                    if (!checkpointfarmmode) break;
                    if (master) SelectMaster();
                    if (!checkpointfarmmode) break;
                    if (activitykey == "DP" || activitykey == "EDP" || activitykey == "EQ") ClearFeats();
                    if (!checkpointfarmmode) break;
                    if (feats) SelectFeats(featlist);
                    if (!checkpointfarmmode) break;

                    InvitePlayer("/invite " + workingusername);

                    statussubtext = "Invite sent. Waiting for player to join.";
                    UpdateTextDisplay();

                    UpdateStatusBar("!FarmCheckpoint... Invite sent to " + workingusername + "... Awaiting their arrival.", UserStatusType.Idle);

                    Point pointcheck = ConvertAspectRatioCoords(95.859, 83.75);
                    Point pointclick = ConvertAspectRatioCoords(75.117, 83.75);

                    SetCursorPos(pointclick.X, pointclick.Y);

                    Task.Delay(1000, OrbitToken).Wait();
                    if (!checkpointfarmmode) break;

                    Color spotcolor = GetColorAt(pointcheck);

                    bool change = false;

                    DateTime time = DateTime.Now.AddHours(1);
                    statussubtext = "Comparing red values to see launch button go red.";
                    UpdateTextDisplay();

                    while (!change)
                    {
                        Color spotcolor2 = GetColorAt(pointcheck);

                        if (Math.Abs(spotcolor.G - spotcolor2.G) > 80)
                        {
                            if (spotcolor2.G != 0) change = true;
                        }

                        if (DateTime.Now > time || OrbitToken.IsCancellationRequested)
                        {
                            client.Rest.SendMessageAsync(message.ChannelId, "Nobody joined. Returning to orbit.");

                            transferingcheckpoint = false;
                            ReturnToCharSelect();

                            UpdateStatusBar("Idle...", UserStatusType.Online);
                            return;
                        }
                        Task.Delay(101, OrbitToken).Wait();
                    }

                    statussubtext = "Button went red. Waiting for it to go back.";
                    UpdateTextDisplay();

                    UpdateStatusBar("!FarmCheckpoint... Join detected, launching momentarily.", UserStatusType.Idle);

                    AwaitColorChange(95.859, 83.75, 1);

                    statussubtext = "Join detected. Launching activity.";
                    UpdateTextDisplay();

                    Task.Delay(2000, OrbitToken).Wait();
                    change = false;

                    while (!change)
                    {
                        if (checkpointfarmmode == false) return;

                        Color spotcolor2 = GetColorAt(pointcheck);

                        if (Math.Abs(spotcolor.G - spotcolor2.G) > 60)
                        {
                            if (spotcolor2.G != 0) change = true;
                        }

                        SendClick(pointclick);

                        if (OrbitToken.IsCancellationRequested)
                        {
                            checkpointfarmmode = false;
                            ReturnToCharSelect();

                            UpdateStatusBar("Idle...", UserStatusType.Online);
                            return;
                        }
                        Task.Delay(250, OrbitToken).Wait();
                    }
                    if (checkpointfarmmode == false) return;

                    statussubtext = "Awaiting first black screen.";
                    UpdateTextDisplay();

                    Task.Delay(2000, OrbitToken).Wait();

                    DateTime bailout = DateTime.Now.AddSeconds(15);

                    while (GetColorAt(ConvertAspectRatioCoords(50, 50)) != black)
                    {
                        if (OrbitToken.IsCancellationRequested)
                        {
                            checkpointfarmmode = false;
                            ReturnToCharSelect();

                            UpdateStatusBar("Idle...", UserStatusType.Online);
                            return;
                        }

                        if (DateTime.Now > bailout) return;
                    }

                    UpdateStatusBar("!FarmCheckpoint... Returning to orbit to prep again.", UserStatusType.Idle);

                    if (!checkpointfarmmode) return;
                    ReturnToCharSelectFast();
                }


            }).Start();
        }

        private static async void CommandListCheckpoint(Message message)
        {
            //make sure reset hasnt happened yet.
            if (checkreset())
            {
                initializing = true;
                client.Rest.SendMessageAsync(message.ChannelId, "Looks like I no longer have that checkpoint due to reset and need to clean everything up. Sorry for the inconvenience. I'm gonna be down for the next 30 minutes or so while I work this out.");
                InitializeCheckpoints();
                GetToDirectorForActivityCoords();
                CleanCheckpoints();
                client.Rest.SendMessageAsync(message.ChannelId, "I'm now done cleaning up post-reset, and I'm funcitonal again now.");

                initializing = false;
                return;
            }

            //list out all checkpoints if no activity is specified. otherwise, list all checkpoints for the given activity. If there are none, explain that.
            string outputraid = "## Raids:\n";
            string outputdungeon = "## Dungeons:\n";
            string outputpantheon = "## Pantheon:\n";
            bool good = false;
            foreach (string key in checkpoints.Keys)
            {
                if (checkpoints[key].Count > 0)
                {
                    good = true;

                    string checkstr = "";
                    int i = 1;
                    foreach (string checkpointname in checkpoints[key].Keys)
                    {
                        checkstr = checkstr + " - [" + i + "]" + checkpointname + "\n";
                        i++;
                    }

                    switch (key)
                    {
                        //raids
                        case "CE":
                            outputraid = outputraid + "### Crotas End:\n" + checkstr + "\n";
                            break;
                        case "masterCE":
                            outputraid = outputraid + "### Crotas End (Master):\n" + checkstr + "\n";
                            break;
                        case "DSC":
                            outputraid = outputraid + "### Deep Stone Crypt:\n" + checkstr + "\n";
                            break;
                        case "DPE":
                            outputraid = outputraid + "### The Desert Perpetual (Epic):\n" + checkstr + "\n";
                            break;
                        case "DP":
                            outputraid = outputraid + "### The Desert Perpetual:\n" + checkstr + "\n";
                            break;
                        case "SE":
                            outputraid = outputraid + "### Salvations Edge:\n" + checkstr + "\n";
                            break;
                        case "masterSE":
                            outputraid = outputraid + "### Salvations Edge (Master):\n" + checkstr + "\n";
                            break;
                        case "RON":
                            outputraid = outputraid + "### Root of Nightmares:\n" + checkstr + "\n";
                            break;
                        case "masterRON":
                            outputraid = outputraid + "### Root of Nightmares (Master):\n" + checkstr + "\n";
                            break;
                        case "KF":
                            outputraid = outputraid + "### King's Fall:\n" + checkstr + "\n";
                            break;
                        case "masterKF":
                            outputraid = outputraid + "### King's Fall (Master):\n" + checkstr + "\n";
                            break;
                        case "VOW":
                            outputraid = outputraid + "### Vow of the Disciple:\n" + checkstr + "\n";
                            break;
                        case "masterVOW":
                            outputraid = outputraid + "### Vow of the Disciple (Master):\n" + checkstr + "\n";
                            break;
                        case "VOG":
                            outputraid = outputraid + "### Vault of Glass:\n" + checkstr + "\n";
                            break;
                        case "masterVOG":
                            outputraid = outputraid + "### Vault of Glass (Master):\n" + checkstr + "\n";
                            break;
                        case "GOS":
                            outputraid = outputraid + "### Garden of Salvation:\n" + checkstr + "\n";
                            break;
                        case "LW":
                            outputraid = outputraid + "### Last Wish:\n" + checkstr + "\n";
                            break;
                        //dungeons
                        case "WR":
                            outputdungeon = outputdungeon + "### Warlords Ruin:\n" + checkstr + "\n";
                            break;
                        case "masterWR":
                            outputdungeon = outputdungeon + "### Warlords Ruin (Master):\n" + checkstr + "\n";
                            break;
                        case "PIT":
                            outputdungeon = outputdungeon + "### Pit of Heresy:\n" + checkstr + "\n";
                            break;
                        case "EQ":
                            outputdungeon = outputdungeon + "### Equilibrium:\n" + checkstr + "\n";
                            break;
                        case "SD":
                            outputdungeon = outputdungeon + "### Sundered Doctrine:\n" + checkstr + "\n";
                            break;
                        case "masterSD":
                            outputdungeon = outputdungeon + "### Sundered Doctrine (Master):\n" + checkstr + "\n";
                            break;
                        case "VH":
                            outputdungeon = outputdungeon + "### Vespers Host:\n" + checkstr + "\n";
                            break;
                        case "masterVH":
                            outputdungeon = outputdungeon + "### Vespers Host (Master):\n" + checkstr + "\n";
                            break;
                        case "GOTD":
                            outputdungeon = outputdungeon + "### Ghosts of the Deep:\n" + checkstr + "\n";
                            break;
                        case "masterGOTD":
                            outputdungeon = outputdungeon + "### Ghosts of the Deep (Master):\n" + checkstr + "\n";
                            break;
                        case "SOTW":
                            outputdungeon = outputdungeon + "### Spire of the Watcher:\n" + checkstr + "\n";
                            break;
                        case "masterSOTW":
                            outputdungeon = outputdungeon + "### Spire of the Watcher (Master):\n" + checkstr + "\n";
                            break;
                        case "D":
                            outputdungeon = outputdungeon + "### Duality:\n" + checkstr + "\n";
                            break;
                        case "masterD":
                            outputdungeon = outputdungeon + "### Duality (Master):\n" + checkstr + "\n";
                            break;
                        case "GOA":
                            outputdungeon = outputdungeon + "### Grasp of Avarice:\n" + checkstr + "\n";
                            break;
                        case "masterGOA":
                            outputdungeon = outputdungeon + "### Grasp of Avarice (Master):\n" + checkstr + "\n";
                            break;
                        case "PR":
                            outputdungeon = outputdungeon + "### Prophecy:\n" + checkstr + "\n";
                            break;
                        case "ST":
                            outputdungeon = outputdungeon + "### Shattered Throne:\n" + checkstr + "\n";
                            break;
                        //pantheon
                        case "CR":
                            outputpantheon = outputpantheon + "### Calus Resplendent:\n" + checkstr + "\n";
                            break;
                        case "MS":
                            outputpantheon = outputpantheon + "### Morgeth Surpassing:\n" + checkstr + "\n";
                            break;
                        case "GAUNTLET":
                            outputpantheon = outputpantheon + "### 7 Encounter Pantheon:\n" + checkstr + "\n";
                            break;
                    }
                }
            }
            if (good)
            {
                if (outputraid == "## Raids:\n") outputraid = "## Raids:\n - None yet.";
                if (outputdungeon == "## Dungeons:\n") outputdungeon = "## Dungeons:\n - None yet.";
                if (outputpantheon == "## Pantheon:\n") outputpantheon = "## Pantheon:\n - None yet.";

                client.Rest.SendMessageAsync(message.ChannelId, outputraid);
                client.Rest.SendMessageAsync(message.ChannelId, outputdungeon);
                client.Rest.SendMessageAsync(message.ChannelId, outputpantheon);
            }
            else
            {
                client.Rest.SendMessageAsync(message.ChannelId, "I currently have no checkpoints. :(");
            }
        }

        private static async void CommandDeleteCheckpoint(Message message) 
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                while (afkcycle)
                {
                    if (OrbitToken.IsCancellationRequested) return;
                }

                if (!oncharselect)
                {
                    if (transferingcheckpoint)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm transferring a checkpoint for " + workingdiscordname + "\n" + "If I'm mistaken in this please run either \"!endhold\" or \"!forceorbit\" depending on how mistaken I am.");
                        return;
                    }
                    if (holdingload)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently holding a load for " + workingdiscordname + "\n" + "If I'm mistaken in this please run either \"!endhold\" or \"!forceorbit\" depending on how mistaken I am.");
                        return;
                    }
                    if (checkpointfarmmode)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently helping " + workingdiscordname + " farm " + activityname + " - " + checkpointname + "\nIf I'm mistaken in this please run either \"!endfarm\" or \"!forceorbit\" depending on how mistaken I am.");
                        return;
                    }
                    if (grabbingcheckpoint)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently grabbing a checkpoint from " + workingdiscordname + "\nIf I'm mistaken in this please run \"!forceorbit\" to help me find my bearings.");
                        return;
                    }
                    if (deletingcheckpoint)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently deleting a checkpoint. Please wait a moment.\nIf I'm mistaken in this please run \"!forceorbit\" to help me find my bearings.");
                        return;
                    }
                    client.Rest.SendMessageAsync(message.ChannelId, "I don't believe I'm in orbit right now.\nIf this is a mistake, please run \"!forceorbit\" for me to rectify the situation.");
                    return;
                }
                
                statusheader = "!DeleteCheckpoint command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                UpdateStatusBar("!DeleteCheckpoint... Validating command.", UserStatusType.Idle);

                deletingcheckpoint = true;
                client.Rest.SendMessageAsync(message.ChannelId, "Making sure I have the checkpoint and everything is correct...");

                //parse command text

                string[] messagechunks = message.Content.Split(" ");

                if (messagechunks.Length < 3)
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "Improper use of the \"!DeleteCheckpoint\" command.\nTry \"!help DeleteCheckpoint\" to learn how to use it.");
                    deletingcheckpoint = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }

                string activity = messagechunks[1].ToUpper();
                string activitykey = activity;

                string[] keys = checkpoints.Keys.ToArray();
                if (!keys.Contains(activity))
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "I appear to not know what activity " + activity + " is.\nTry \"!help DeleteCheckpoint\" to learn how to use this command, or \"!activities\" to see what activities are available.");
                    deletingcheckpoint = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }

                bool master = false;
                string ckpointname = messagechunks[2];

                workingdiscordname = message.Author.Username;
                if (message.Author.GlobalName != null) workingdiscordname = message.Author.GlobalName;
                int namestartindex = 3;

                if (messagechunks[2].ToLower() == "master")
                {
                    namestartindex = 4;
                    checkpointname = messagechunks[3];
                    master = true;
                    if (!keys.Contains("master" + activity))
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "That activity doesn't appear to have a master mode.\nTry \"!help DeleteCheckpoint\" to learn how to use this command.");
                        deletingcheckpoint = false;

                        statusheader = "Idle...";
                        statussubtext = "";
                        UpdateTextDisplay();

                        UpdateStatusBar("Idle...", UserStatusType.Online);
                        return;
                    }
                    else
                    {
                        activitykey = "master" + activity;
                        ckpointname = messagechunks[3];
                    }
                }

                Regex rgx = new Regex("[^a-zA-Z0-9 -]");
                ckpointname = rgx.Replace(ckpointname, "");

                int charslot = 0;
                if (checkpoints[activitykey].Keys.Contains(ckpointname))
                {
                    charslot = checkpoints[activitykey][ckpointname];
                }
                else
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "I don't currently have a checkpoint with the name " + ckpointname + " in my save data.\nTry \"!help DeleteCheckpoint\" to learn how to use this command, or \"!listcheckpoints\" to see what checkpoints I have.");
                    deletingcheckpoint = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }

                UpdateStatusBar("!DeleteCheckpoint... Navigating to activity.", UserStatusType.Idle);

                SelectChar(charslot);
                SelectDirector();
                SelectPortal();
                SelectActivity(activity);
                if (master) SelectMaster();

                UpdateStatusBar("!DeleteCheckpoint... Removing checkpoint.", UserStatusType.Idle);
                removecheckpoint();

                checkpoints[activitykey].Remove(ckpointname);
                savecheckpoints();

                UpdateStatusBar("!DeleteCheckpoint... Returning to character select...", UserStatusType.Idle);

                ReturnToCharSelect();

                UpdateStatusBar("Idle...", UserStatusType.Online);

            }).Start();

                //check to see if the checkpoint exists. if it does, ask for confirmation. if not, make sure the user typed it correctly.
                //also make sure im not already waiting for confirmation somewhere.
                //if confirmed, delete checkpoint.
        }

        private static async void CommandEndHoldCheckpoint(Message message) 
        {
            //check to see if im even holdling a checkpoint. 
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                //verify.
                verifying = true;
                verifylevel = 0;

                string oldstatus = statusheader;
                string oldsubtext = statussubtext;

                statusheader = "Processing !EndHold command:";
                statussubtext = "Awaiting first verification.";
                UpdateTextDisplay();

                client.Rest.SendMessageAsync(message.ChannelId, "Are you sure you're done holding the load? If you're doing this to a load someone else is holding please make sure they're done.\nSend \"!verify\" to confirm. If no message is sent in 60 seconds I'll go back to idleing.");

                DateTime timeout = DateTime.Now.AddMinutes(1);
                while (verifylevel == 0)
                {
                    if (!verifying) return;
                    if (DateTime.Now > timeout)
                    {
                        verifying = false;
                        verifylevel = 0;
                        client.Rest.SendMessageAsync(message.ChannelId, "No valid response given in time. Continuing without returning to orbit.");

                        statusheader = oldstatus;
                        statussubtext = oldsubtext;
                        UpdateTextDisplay();
                    }
                }
                holdingload = false;
            }).Start();
        }

        private static async void CommandHoldCheckpoint(Message message) 
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                while (afkcycle)
                {
                    if (OrbitToken.IsCancellationRequested) return;
                }

                if (!oncharselect)
                {
                    if (transferingcheckpoint)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm transferring a checkpoint for " + workingdiscordname + "\n" + "If I'm mistaken in this please run either \"!endhold\" or \"!forceorbit\" depending on how mistaken I am.");
                        return;
                    }
                    if (holdingload)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently holding a load for " + workingdiscordname + "\n" + "If I'm mistaken in this please run either \"!endhold\" or \"!forceorbit\" depending on how mistaken I am.");
                        return;
                    }
                    if (checkpointfarmmode)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently helping " + workingdiscordname + " farm " + activityname + " - " + checkpointname + "\nIf I'm mistaken in this please run either \"!endfarm\" or \"!forceorbit\" depending on how mistaken I am.");
                        return;
                    }
                    if (grabbingcheckpoint)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently grabbing a checkpoint from " + workingdiscordname + "\nIf I'm mistaken in this please run \"!forceorbit\" to help me find my bearings.");
                        return;
                    }
                    if (deletingcheckpoint)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently deleting a checkpoint. Please wait a moment.\nIf I'm mistaken in this please run \"!forceorbit\" to help me find my bearings.");
                        return;
                    }
                    client.Rest.SendMessageAsync(message.ChannelId, "I don't believe I'm in orbit right now.\nIf this is a mistake, please run \"!forceorbit\" for me to rectify the situation.");
                    return;
                }

                statusheader = "!HoldLoad command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                UpdateStatusBar("!HoldLoad... Validating command.", UserStatusType.Idle);

                holdingload = true;

                //figure out what character to grab the checkpoint on. if my checkpoints are full, bail.
                string[] messagechunks = message.Content.Split(" ");
                //!GrabCheckpoint BungieUsername#0000
                if (messagechunks.Length < 2)
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "Improper use of the \"!holdload\" command.\nTry \"!help holdload\" to learn how to use it.");
                    holdingload = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }

                string[] nameID = messagechunks.Last().Split("#");
                if (nameID.Length == 1 || nameID.Last().Split('#').Last().Length != 4)
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "For the \"!holdload\" command to work, I need the 4 number hashtag after your guardians name.\nTry \"!help holdload\" to learn how to use it.");
                    grabbingcheckpoint = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }

                workingusername = "";
                for (int i = 1; i < messagechunks.Length; i++)
                {
                    if (workingusername == "") workingusername = workingusername + messagechunks[i];
                    else workingusername = workingusername + " " + messagechunks[i];
                }

                int charslot = 1;

                //activity for the activity name (shorthand + master)
                //charslot for which character the checkpoint is stored on.
                //master (bool) to know if its master mode or not

                statusheader = "!HoldLoad command:";
                statussubtext = "Attempting to join...";
                UpdateTextDisplay();

                client.Rest.SendMessageAsync(message.ChannelId, "Checking to see if I can join...");
                SelectChar(charslot);

                UpdateStatusBar("!HoldLoad... Joining fireteam.", UserStatusType.Idle);
                bool worked = JoinFireteamFromOrbit("/join " + workingusername);
                if (!worked)
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "Looks like your fireteam is currently unavailable. Returning to idling.");
                    grabbingcheckpoint = false;
                    ReturnToCharSelect();

                    UpdateStatusBar("Idle...", UserStatusType.Online);

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();
                }
                else
                {

                    UpdateStatusBar("!HoldLoad... Currently holding load for " + workingdiscordname + ". Run !stopholding to stop.", UserStatusType.Idle);
                    statusheader = "!HoldLoad command:";
                    statussubtext = "Boots on ground. Going to AFK macro.";
                    UpdateTextDisplay();

                    client.Rest.SendMessageAsync(message.ChannelId, "Now boots on the ground and holding the load. Remember to run \"!stopholding\" when you want me to stop.");

                    //navigate to collections
                    _controller.SetButtonState(Xbox360Button.B, true);
                    Task.Delay(101, OrbitToken).Wait();
                    _controller.SetButtonState(Xbox360Button.B, false);
                    Task.Delay(101, OrbitToken).Wait();
                    _controller.SetButtonState(Xbox360Button.B, true);
                    Task.Delay(101, OrbitToken).Wait();
                    _controller.SetButtonState(Xbox360Button.B, false);
                    Task.Delay(101, OrbitToken).Wait();
                    //start, lb, lb, click lore tab
                    _controller.SetButtonState(Xbox360Button.Start, true);
                    Task.Delay(101, OrbitToken).Wait();
                    _controller.SetButtonState(Xbox360Button.Start, false);
                    Task.Delay(101, OrbitToken).Wait();
                    _controller.SetAxisValue(Xbox360Axis.LeftThumbY, STICK_BACK);
                    Task.Delay(1000, OrbitToken).Wait();

                    _controller.SetButtonState(Xbox360Button.LeftShoulder, true);
                    Task.Delay(101, OrbitToken).Wait();
                    _controller.SetButtonState(Xbox360Button.LeftShoulder, false);
                    Task.Delay(400, OrbitToken).Wait();
                    _controller.SetAxisValue(Xbox360Axis.LeftThumbY, STICK_CENTER);
                    Task.Delay(101, OrbitToken).Wait();

                    _controller.SetButtonState(Xbox360Button.LeftShoulder, true);
                    Task.Delay(101, OrbitToken).Wait();
                    _controller.SetButtonState(Xbox360Button.LeftShoulder, false);
                    Task.Delay(1000, OrbitToken).Wait();

                    SendClick(new Point(0, 0));
                    Task.Delay(1000, OrbitToken).Wait();
                    SetCursorPos(ConvertAspectRatioCoords(68.395375, 60).X, ConvertAspectRatioCoords(68.395375, 60).Y);
                    Task.Delay(1000, OrbitToken).Wait();

                    _controller.SetButtonState(Xbox360Button.A, true);
                    Task.Delay(101, OrbitToken).Wait();
                    _controller.SetButtonState(Xbox360Button.A, false);
                    Task.Delay(101, OrbitToken).Wait();
                    _controller.SetButtonState(Xbox360Button.A, true);
                    Task.Delay(101, OrbitToken).Wait();
                    _controller.SetButtonState(Xbox360Button.A, false);
                    Task.Delay(101, OrbitToken).Wait();

                    while (holdingload)
                    {
                        _controller.SetButtonState(Xbox360Button.LeftShoulder, true);
                        Task.Delay(101, OrbitToken).Wait();
                        _controller.SetButtonState(Xbox360Button.LeftShoulder, false);
                        Task.Delay(3000, OrbitToken).Wait();
                        if (OrbitToken.IsCancellationRequested) return;
                        if (!holdingload) break;

                        _controller.SetButtonState(Xbox360Button.RightShoulder, true);
                        Task.Delay(101, OrbitToken).Wait();
                        _controller.SetButtonState(Xbox360Button.RightShoulder, false);
                        Task.Delay(3000, OrbitToken).Wait();
                        if (OrbitToken.IsCancellationRequested) return;
                    }

                    _controller.SetButtonState(Xbox360Button.B, true);
                    Task.Delay(101, OrbitToken).Wait();
                    _controller.SetButtonState(Xbox360Button.B, false);
                    Task.Delay(101, OrbitToken).Wait();
                    _controller.SetButtonState(Xbox360Button.B, true);
                    Task.Delay(101, OrbitToken).Wait();
                    _controller.SetButtonState(Xbox360Button.B, false);
                    Task.Delay(101, OrbitToken).Wait();
                    _controller.SetButtonState(Xbox360Button.B, true);
                    Task.Delay(101, OrbitToken).Wait();
                    _controller.SetButtonState(Xbox360Button.B, false);
                    Task.Delay(101, OrbitToken).Wait();

                    ReturnToCharSelect();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                }
            }).Start();
        }

        private static async void CommandGrabCheckpoint(Message message)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                //make sure reset hasnt happened yet.
                if (checkreset())
                {
                    initializing = true;
                    client.Rest.SendMessageAsync(message.ChannelId, "Looks like I no longer have that checkpoint due to reset and need to clean everything up. Sorry for the inconvenience. I'm gonna be down for the next 30 minutes or so while I work this out.");
                    InitializeCheckpoints();
                    GetToDirectorForActivityCoords();
                    CleanCheckpoints();
                    client.Rest.SendMessageAsync(message.ChannelId, "I'm now done cleaning up post-reset, and I'm funcitonal again now.");

                    initializing = false;
                    return;
                }

                while (afkcycle)
                {
                    if (OrbitToken.IsCancellationRequested) return;
                }

                if (!oncharselect)
                {
                    if (transferingcheckpoint)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm transferring a checkpoint for " + workingdiscordname + "\n" + "If I'm mistaken in this please run either \"!endhold\" or \"!forceorbit\" depending on how mistaken I am.");
                        return;
                    }
                    if (holdingload)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently holding a load for " + workingdiscordname + "\n" + "If I'm mistaken in this please run either \"!endhold\" or \"!forceorbit\" depending on how mistaken I am.");
                        return;
                    }
                    if (checkpointfarmmode)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently helping " + workingdiscordname + " farm " + activityname + " - " + checkpointname + "\nIf I'm mistaken in this please run either \"!endfarm\" or \"!forceorbit\" depending on how mistaken I am.");
                        return;
                    }
                    if (grabbingcheckpoint)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently grabbing a checkpoint from " + workingdiscordname + "\nIf I'm mistaken in this please run \"!forceorbit\" to help me find my bearings.");
                        return;
                    }
                    if (deletingcheckpoint)
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "I'm currently deleting a checkpoint. Please wait a moment.\nIf I'm mistaken in this please run \"!forceorbit\" to help me find my bearings.");
                        return;
                    }
                    client.Rest.SendMessageAsync(message.ChannelId, "I don't believe I'm in orbit right now.\nIf this is a mistake, please run \"!forceorbit\" for me to rectify the situation.");
                    return;
                }

                statusheader = "!GrabCheckpoint command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                UpdateStatusBar("!GrabCheckpoint... Validating command.", UserStatusType.Idle);

                grabbingcheckpoint = true;

                //figure out what character to grab the checkpoint on. if my checkpoints are full, bail.
                string[] messagechunks = message.Content.Split(" ");
                //!GrabCheckpoint [activity shorthand (!activities)] [(optional)master] [single word name for the checkpoint of your choosing.  a-z, 1-9 only] BungieUsername#0000
                if (messagechunks.Length < 4)
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "Improper use of the \"!grabcheckpoint\" command.\nTry \"!help grabcheckpoint\" to learn how to use it.");
                    grabbingcheckpoint = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }

                string[] nameID = messagechunks.Last().Split("#");
                if (nameID.Length == 1 || nameID.Last().Split('#').Last().Length != 4)
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "For the \"!grabcheckpoint\" command to work, I need the 4 number hashtag after your guardians name.\nTry \"!help grabcheckpoint\" to learn how to use it.");
                    grabbingcheckpoint = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }

                string activity = messagechunks[1].ToUpper();
                string activitykey = activity;

                string[] keys = checkpoints.Keys.ToArray();
                if (!keys.Contains(activity))
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "I appear to not know what activity " + activity + " is.\nTry \"!help grabcheckpoint\" to learn how to use this command, or \"!activities\" to see what activities are available.");
                    grabbingcheckpoint = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }
                bool master = false;
                string ckpointname = messagechunks[2];

                workingdiscordname = message.Author.Username;
                if (message.Author.GlobalName != null) workingdiscordname = message.Author.GlobalName;
                int namestartindex = 3;

                if (messagechunks[2].ToLower() == "master")
                {
                    namestartindex = 4;
                    ckpointname = messagechunks[3];
                    master = true;
                    if (!keys.Contains("master" + activity))
                    {
                        client.Rest.SendMessageAsync(message.ChannelId, "That activity doesn't appear to have a master mode.\nTry \"!help grabcheckpoint\" to learn how to use this command.");
                        grabbingcheckpoint = false;

                        statusheader = "Idle...";
                        statussubtext = "";
                        UpdateTextDisplay();

                        UpdateStatusBar("Idle...", UserStatusType.Online);
                        return;
                    }
                    else
                    {
                        activitykey = "master" + activity;
                    }
                }

                workingusername = "";
                for (int i = namestartindex; i < messagechunks.Length; i++)
                {
                    if (workingusername == "") workingusername = workingusername + messagechunks[i];
                    else workingusername = workingusername + " " + messagechunks[i];
                }

                Regex rgx = new Regex("[^a-zA-Z0-9 -]");
                ckpointname = rgx.Replace(ckpointname, "");

                int charslot = 0;
                if (checkpoints[activitykey].Keys.Contains(ckpointname))
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "I already have a checkpoint with the name " + ckpointname + " in my save data.\nTry \"!help grabcheckpoint\" to learn how to use this command.");
                    grabbingcheckpoint = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }
                if (checkpoints[activitykey].Keys.Count == 3)
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "I already have 3 checkpoints for that activity in my record.\nTry \"!help deletecheckpoint\" to learn how to delete a checkpoint so that you may overwrite it, and \"!listcheckpoints\" to see what checkpoints I have.");
                    grabbingcheckpoint = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }
                else
                {
                    charslot = 1;
                    //charslot = checkpoints[activitykey].Count + 1;
                    foreach (string tk in checkpoints[activitykey].Keys)
                    {
                        if (checkpoints[activitykey][tk] == 1) charslot = 2;
                        if (checkpoints[activitykey][tk] == 2) charslot = 3;
                    }
                }

                if (ckpointname == "")
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "Somehow you've managed to give me a checkpoint name that when filtered for only numbers and letters is an empty string.\nTry \"!help grabcheckpoint\" to learn how to use this command.");
                    grabbingcheckpoint = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }

                //activity for the activity name (shorthand + master)
                //charslot for which character the checkpoint is stored on.
                //master (bool) to know if its master mode or not

                statusheader = "!GrabCheckpoint command:";
                statussubtext = "Attempting to join...";
                UpdateTextDisplay();

                UpdateStatusBar("!GrabCheckpoint... Attempting to join " + workingusername + ".", UserStatusType.Idle);

                client.Rest.SendMessageAsync(message.ChannelId, "Checking to see if I can join...");
                SelectChar(charslot);
                bool worked = JoinFireteamFromOrbit("/join " + workingusername);
                if (!worked)
                {
                    client.Rest.SendMessageAsync(message.ChannelId, "Looks like your fireteam is currently unavailable. Returning to idling.");
                    grabbingcheckpoint = false;
                    ReturnToCharSelect();

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                }
                else
                {
                    //make sure its not an error code please TODO - not sure how to synthesize an error code here honestly.

                    //wait for wipe and then return to orbit

                    statusheader = "!GrabCheckpoint command:";
                    statussubtext = "Boots on ground. Awaiting wipe screen.";
                    UpdateTextDisplay();

                    UpdateStatusBar("!GrabCheckpoint... Awaiting wipe :3", UserStatusType.Idle);

                    awaittext("fromlast", ConvertAspectRatioCoords(30.4296875, 16.3888888), ConvertAspectRatioCoords(39.6484375, 19.236111111)); //779 236 1015 277

                    statusheader = "!GrabCheckpoint command:";
                    statussubtext = "Wipe screen found. Waiting for wipe screen to clear.";
                    UpdateTextDisplay();

                    AwaitColorChange(50, 50, 2);

                    if (!master) client.Rest.SendMessageAsync(message.ChannelId, "Wipe detected. Checkpoint " + ckpointname + " grabbed for " + activity + " from " + workingusername + ".\n Returning to orbit to idle.");
                    else client.Rest.SendMessageAsync(message.ChannelId, "Wipe detected. Checkpoint " + ckpointname + " grabbed for master " + activity + " from " + workingusername + ".\n Returning to orbit to idle.");

                    checkpoints[activitykey].Add(ckpointname,charslot);
                    savecheckpoints();

                    grabbingcheckpoint = false;
                    bootsonground = false;

                    UpdateStatusBar("!GrabCheckpoint... Returning to orbit.", UserStatusType.Idle);
                    ReturnToCharSelectFast();

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                }
            }).Start();
        }

        private static async void CommandActivities(Message message)
        {
            client.Rest.SendMessageAsync(message.ChannelId, "## Raids:\n" +
                " - CE (Crota's End) \n" +
                " - DSC (Deep Stone Crypt \n" +
                " - DPE (Desert Perpetual Epic) \n" +
                " - DP (Desert Perpetual) \n" +
                " - SE (Salvations Edge) \n" +
                " - RON (Root of Nightmares) \n" +
                " - KF (King's Fall) \n" +
                " - VOW (Vow of the Disciple) \n" +
                " - VOG (Vault of Glass) \n" +
                " - GOS (Garden of Salvation) \n" +
                " - LW (Last Wish) \n \n" +
                "## Dungeons: \n" +
                " - WR (Warlord's Ruin) \n" +
                " - PIT (Pit of Heresy) \n" +
                " - EQ (Equilibrium) \n" +
                " - SD (Sundered Doctrine) \n" +
                " - VH (Vesper's Host) \n" +
                " - GOTD (Ghosts of the Deep \n" +
                " - SOTW (Spire of the Watcher) \n" +
                " - D (Duality) \n" +
                " - GOA (Grasp of Avarice) \n" +
                " - PR (Prophecy) \n" +
                " - ST (The Shattered Throne) \n\n" +
                "## Pantheon: \n" +
                " - CR (Calus Resplendent) \n" +
                " - MS (Morgeth Surpassing) \n" +
                " - GAUNTLET (Full 7 boss pantheon)");
        }

        private static async void CommandListCommands(Message message)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            client.Rest.SendMessageAsync(message.ChannelId,
                "## Available commands:\n" +
                "### !ListCommands:\n" +
                " - I'll list all available commands and their definitions.\n" +
                " - Usage: !ListCommands\n" +
                "### !Activities\n" +
                " - I'll list all available activities.\n" +
                " - usage: !Activities\n" +
                "### !HoldLoad: \n" +
                " - I'll hold a load by joining on the person until the !EndHold command is run.\n" +
                " - usage: !HoldLoad BungieUsername#0000\n" +
                "### !EndHold: \n" +
                " - First I will confirm that you do want to end the hold, with !verify.\n" +
                " - After that, I'll return to orbit and enter idle mode.\n" +
                " - usage: !EndHold \n" +
                "### !GrabCheckpoint: \n" +
                " - Has me join on the username given with the command, and then wait for a wipe so that I have the checkpoint, at which point I'll then return to orbit and idle.\n" +
                " - usage: !GrabCheckpoint [activity shorthand (!activities)] [(optional)master] [single word name for the checkpoint of your choosing. a-z, 1-9 only] BungieUsername#0000 \n" +
                " - example: !GrabCheckpoint WR ogre ItAvvy#7006\n" +
                " - If I already have a checkpoint in that activity on all 3 characters I will list them and ask you to delete one using !deletecheckpoint.").Wait();
            client.Rest.SendMessageAsync(message.ChannelId,
                "### !DeleteCheckpoint:\n" +
                " - I'll delete a checkpoint from a given activity with a given name so that a new checkpoint may be gotten on that character.\n" +
                " - usage: !DeleteCheckpoint [activity shorthand (!activities)] [(optional)master] [single word name of the checkpoint. a-z, 1-9 only] \n" +
                " - example: !DeleteCheckpoint WR master ogre\n" +
                "### !ListCheckpoints: \n" +
                " - Lists out all checkpoints on a given activity, and specifies master in cases where its applicable.\n" +
                " - usage: !ListCheckpoint [activity shorthand (!activities)]\n" +
                " - use !ListCheckpoint All - to see all available checkpoints across all activities\n" +
                "### !FarmCheckpoint: \n" +
                " - I'll load the character the given checkpoint is on, and I'll wait in orbit for you to join. The moment you join I'll launch the activity, transferring the checkpoint on load-in. Then, I will return to orbit to wait to launch again. Use !EndFarm to end the farm. If you specify feats with the optional modifier, I will try to launch the checkpoint with those feats if applicable.\n" +
                " - Viable Feats: Token, Phase, Battalions, Challenges, and Cutthroat. \n" +
                " - usage: !FarmCheckpoint [activity shorthand (!activities)] [(optional)master] [(optional)feats:feat1name,feat2name,etc...] [single word name for the checkpoint of your choosing. a-z, 1-9 only] BungieUsername#0000 \n" +
                " - example: !FarmCheckpoint EQ feats:tokenlimit,phaselimit shockyhands ItAvvy#7006\n" +
                "### !CleanCheckpoints\n" +
                " - I will go thru, activity by activity, both normal and master and delete any erronious checkpoints I may have that I don't have record of.\n" +
                " - This does require you to verify that you want to do it beforehand, as it takes a while to go thru everything.\n" +
                " - Usage: !CleanCheckpoints").Wait();
            client.Rest.SendMessageAsync(message.ChannelId,
                "### !EndFarm: \n" +
                " - I'll stop farming the given activity and shift into idle mode. I will then ask for you to run !verify to verify that you do in fact want to end the farm.\n" +
                " - Usage: !EndFarm\n" +
                "### !ForceWipe: \n" +
                " - If applicable, I'll fire a rocket at the ground to force a wipe.\n" +
                " - usage: !ForceWipe\n" +
                "### !Help: \n" +
                " - Allows you to ask about any other command and get the above descriptions, but with much less text-spam. Or just use !help to see this list.\n" +
                " - Usage: !help [other command]\n" +
                "### !TransferCheckpoint \n" +
                " - Has me load into a checkpoint only a single time after someone joins my lobby, so that I may transfer the checkpoint to them, and then I'll return to idle.\n" +
                " - Usage: !TransferCheckpoint [activity shorthand(!activities)] [(optional)master] [single word name given to the checkpoint. a-z, 1-9 only] BungieUsername#0000\n" +
                "### !ForceOrbit\n" +
                " - Has me attempt to change characters thru the settings menu, to rescue myself from a softlock of some kind. May not always work. At which point I will forget everything I was doing, and will need to be set back up for farms and stuff.\n" +
                " - I will ask for confirmation twice before doing this.\n" +
                " - Usage: !ForceOrbit\n" +
                "### !FlyInCheckpointTransfer\n" +
                " - Transfers a checkpoint from you, to me, the hard way without using a darkness zone.\n" +
                " - Warning, this requires a bit of cooperation, and will only work if your fireteam is set to open.\n" +
                " - First, navigate on your director to the activity that has the checkpoint you want to transfer on it, and wait in orbit. Then run this command.\n" +
                " - After you run the command I will attempt to join on you.\n" +
                " - Then, I'll ask you to launch the activity, open your inventory, navigate to \"change character\" in your settings, click it, and then have you wait to confirm.\n" +
                " - Once my screen goes black, I'll send a chat message telling you to hit confirm to change characters.\n" +
                " - Once I'm boots on the ground I will return to orbit, verify that I do have the checkpoint, and echo the result here.\n" +
                " - Usage: !TransferCheckpoint [activity shorthand (!activities)] [(optional)master] [single word name of the checkpoint.  a-z, 1-9 only] BungieUsername#0000\n" +
                "### !CleanCheckpoints\n" +
                " - I will go thru, activity by activity, both normal and master and delete any erronious checkpoints I may have that I don't have record of.\n" +
                " - This does require you to verify that you want to do it beforehand, as it takes about 30 minutes to go thru everything.\n" +
                " - Usage: !CleanCheckpoints");

#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private static Color GetColorAt(Point coordinates)
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

        private static async void GetActivityText()
        {
            bool good = true;
            //convert to 16x9 for u.i. with a border for all math.
            int width = d2window.Right - d2window.Left;
            int height = d2window.Bottom - d2window.Top;
            double ratio = width / height;
            double desiredratio = 16 / 9;
            int leftbuffer = 0;
            int topbuffer = 0;

            double iconpercentageX = 19.68;
            double iconpercentageY = 6;

            double firstemblemX = 12.14;
            double firstemblemY = 31.94;

            double horgap = 6.69;
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
            _controller.SetButtonState(Xbox360Button.A, true);
            Task.Delay(101).Wait();
            _controller.SetButtonState(Xbox360Button.A, false);

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
                    Task.Delay(100).Wait();
                }
                ypos = ypos + iconheight + iconYgap;
            }

            Task.Delay(100).Wait();

            _controller.SetButtonState(Xbox360Button.Down, true);
            Task.Delay(101).Wait();
            _controller.SetButtonState(Xbox360Button.Down, false);
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

            _controller.SetButtonState(Xbox360Button.RightShoulder, true);
            Task.Delay(101).Wait();
            _controller.SetButtonState(Xbox360Button.RightShoulder, false);
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
            _controller.SetButtonState(Xbox360Button.Down, true);
            Task.Delay(101).Wait();
            _controller.SetButtonState(Xbox360Button.Down, false);

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

            _controller.SetButtonState(Xbox360Button.RightShoulder, true);
            Task.Delay(101).Wait();
            _controller.SetButtonState(Xbox360Button.RightShoulder, false);

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
            _controller.SetButtonState(Xbox360Button.B, true);
            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.B, false);

            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.B, true);
            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.B, false);

            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.B, true);
            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.B, false);

            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.B, true);
            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.B, false);

            ReturnToCharSelect();

            if(good == false)
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

        private static Point ConvertAspectRatioCoords(double xper, double yper)
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

            return ocrtext;
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
                case "pantheoncalusresplenden":
                    return "CR";
                case "pantheonmorgethsurpassir":
                    return "MS";
                case "pantheoninsurrectionprimerevolutionary":
                    return "GAUNTLET";
            }
            return "fail";
        }

        public static void ReturnToCharSelect()
        {
            statussubtext = "Return to orbit: Making sure I'm taking controller input.";
            UpdateTextDisplay();

            int width = d2window.Right - d2window.Left;
            int height = d2window.Bottom - d2window.Top;

            int iconwidth = (int)Math.Round(width * 0.071484);
            int iconheight = (int)Math.Round(height * 0.025695);
            Point startcoords = ConvertAspectRatioCoords(35.195, 49.375);
            int xpos = startcoords.X;
            int ypos = startcoords.Y;

            //make sure im in controller mode
            _controller.SetButtonState(Xbox360Button.Y, true);
            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.Y, false);
            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.Y, true);
            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.Y, false);

            statussubtext = "Return to orbit: Double checking I'm not already on character select.";
            UpdateTextDisplay();
            bool works = false;
            string ocrstring = "";

            while (!works)
            {
                if (OrbitToken.IsCancellationRequested) return;
                try
                {
                    Thread.Sleep(101);
                    _controller.SetButtonState(Xbox360Button.B, true);
                    Thread.Sleep(101);
                    _controller.SetButtonState(Xbox360Button.B, false);
                    Thread.Sleep(2500);
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
                    works = true;
                }
                catch
                {

                }
            }

            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.B, true);
            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.B, false);

            if (ocrstring.Contains("areyou"))
            {
                statussubtext = "Return to orbit: Turns out I'm already there. Going to idle...";
                UpdateTextDisplay();

                Thread.Sleep(500);
                SetCursorPos(ConvertAspectRatioCoords(50, 50).X, ConvertAspectRatioCoords(50, 50).Y);
                SendClick(new Point(50, 50));
                Thread.Sleep(200);
                SendClick(new Point(50, 50));
                oncharselect = true;

                statusheader = "Idle...";
                statussubtext = "";
                UpdateTextDisplay();
                return;
            }
            statussubtext = "Return to orbit: Doing menuing to get back to character select...";
            UpdateTextDisplay();

            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.Start, true);
            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.Start, false);
            AwaitColorChange(95, 5, 1);
            Thread.Sleep(2000);
            _controller.SetButtonState(Xbox360Button.RightShoulder, true);
            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.RightShoulder, false);
            AwaitColorChange(95, 10, 1);
            Thread.Sleep(300);
            _controller.SetButtonState(Xbox360Button.RightShoulder, true);
            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.RightShoulder, false);
            AwaitColorChange(95, 10, 1);
            Thread.Sleep(300);
            _controller.SetButtonState(Xbox360Button.RightShoulder, true);
            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.RightShoulder, false);
            Thread.Sleep(1000);
            _controller.SetButtonState(Xbox360Button.Down, true);
            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.Down, false);
            Thread.Sleep(400);
            _controller.SetButtonState(Xbox360Button.Down, true);
            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.Down, false);
            Thread.Sleep(400);
            _controller.SetButtonState(Xbox360Button.Down, true);
            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.Down, false);
            Thread.Sleep(400);
            _controller.SetButtonState(Xbox360Button.Down, true);
            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.Down, false);
            Thread.Sleep(1000);


            SendClick(ConvertAspectRatioCoords(84.21, 23.26));
            Thread.Sleep(101);
            SetCursorPos(ConvertAspectRatioCoords(84.21, 23.26).X, ConvertAspectRatioCoords(84.21, 23.26).Y);
            Thread.Sleep(101);

            for (int i = 0; i < 4; i++)
            {
                _controller.SetButtonState(Xbox360Button.A, true);
                Thread.Sleep(101);
                _controller.SetButtonState(Xbox360Button.A, false);
                Thread.Sleep(101);
            }

            Thread.Sleep(200);
            awaittext("ExittoDesktop",ConvertAspectRatioCoords(0.5,95.972222222),ConvertAspectRatioCoords(14.0625,98.75));
            SetCursorPos(ConvertAspectRatioCoords(50, 50).X, ConvertAspectRatioCoords(50, 50).Y);
            SendClick(ConvertAspectRatioCoords(50, 50));
            Thread.Sleep(1000);

            statusheader = "Idle...";
            statussubtext = "";
            UpdateTextDisplay();

            oncharselect = true;
        }

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
            _controller.SetButtonState(Xbox360Button.Y, true);
            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.Y, false);
            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.Y, true);
            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.Y, false);

            statussubtext = "Return to orbit fast: Doing menuing to get back to character select...";
            UpdateTextDisplay();

            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.Start, true);
            Thread.Sleep(101);
            _controller.SetButtonState(Xbox360Button.Start, false);
            AwaitColorChange(95, 5, 1);
            SendClick(ConvertAspectRatioCoords(84.21, 23.26));
            Thread.Sleep(1000);

            Point selectpos = ConvertAspectRatioCoords(89.2578125, 4.4444444444);
            SetCursorPos(selectpos.X,selectpos.Y);
            SendClick(selectpos);

            AwaitColorChange(95, 10, 1);
            Thread.Sleep(300);

            selectpos = ConvertAspectRatioCoords(14.8046875, 63.75);
            SetCursorPos(selectpos.X, selectpos.Y);
            SendClick(selectpos);

            Thread.Sleep(400);

            SetCursorPos(ConvertAspectRatioCoords(84.21, 23.26).X, ConvertAspectRatioCoords(84.21, 23.26).Y);
            Thread.Sleep(101);

            for (int i = 0; i < 4; i++)
            {
                _controller.SetButtonState(Xbox360Button.A, true);
                Thread.Sleep(101);
                _controller.SetButtonState(Xbox360Button.A, false);
                Thread.Sleep(101);
            }

            Thread.Sleep(200);
            awaittext("ExittoDesktop", ConvertAspectRatioCoords(0.5, 95.972222222), ConvertAspectRatioCoords(14.0625, 98.75));
            SetCursorPos(ConvertAspectRatioCoords(50, 50).X, ConvertAspectRatioCoords(50, 50).Y);
            SendClick(ConvertAspectRatioCoords(50, 50));
            Thread.Sleep(1000);
            oncharselect = true;

            statusheader = "Idle...";
            statussubtext = "";
            UpdateTextDisplay();
        }

        public static BitmapSource ConvertBitmapToBitmapSource(System.Drawing.Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);

            return bitmapSource;
        }

        /// <summary>
        /// this assumes you're already on character select screen. Counts from 1, not 0
        /// </summary>
        /// <param name="charslot"></param>
        private static async void SelectChar(int charslot)
        {
            oncharselect = false;
            int gap = (int)Math.Round((charslot - 1) * 10.278);

            statussubtext = "Selecting character...";
            UpdateTextDisplay();

            SetCursorPos(ConvertAspectRatioCoords(60.859, 41.806 + gap).X, ConvertAspectRatioCoords(60.859, 41.806 + gap).Y);
            SendClick(ConvertAspectRatioCoords(60.859, 41.806 + gap));
            Task.Delay(101, OrbitToken).Wait();
            SendClick(ConvertAspectRatioCoords(60.859, 41.806 + gap));
            Task.Delay(800, OrbitToken).Wait();
            black = GetColorAt(new Point(50, 50));
            AwaitColorChange(50, 50, 2);
            Task.Delay(3000, OrbitToken).Wait();
            //now in orbit
        }

        /// <summary>
        /// this assumes you're already in orbit.
        /// </summary>
        private static async void SelectDirector()
        {
            statussubtext = "Getting to director...";
            UpdateTextDisplay();

            _controller.SetButtonState(Xbox360Button.Back, true);
            Task.Delay(101, OrbitToken).Wait();
            _controller.SetButtonState(Xbox360Button.Back, false);
            Task.Delay(101, OrbitToken).Wait();
            AwaitColorChange(56.52, 87.08, 2);
            Task.Delay(2500, OrbitToken).Wait();

            //now on director
        }

        /// <summary>
        /// this assumes you're already on the director.
        /// </summary>
        private static async void SelectPortal()
        {
            statussubtext = "Locating portal...";
            UpdateTextDisplay();

            SendClick(ConvertAspectRatioCoords(50, 50));
            Task.Delay(101, OrbitToken).Wait();
            SendClick(ConvertAspectRatioCoords(50, 50));
            Task.Delay(101, OrbitToken).Wait();
            SetCursorPos(ConvertAspectRatioCoords(56.52, 87.08).X, ConvertAspectRatioCoords(56.52, 87.08).Y);
            Task.Delay(101, OrbitToken).Wait();
            SendClick(ConvertAspectRatioCoords(56.52, 87.08));
            AwaitColorChange(50, 85, 2);
            Task.Delay(1000, OrbitToken).Wait();
            //now on portal
        }

        /// <summary>
        /// this assumes you're on the portal, on the first page.
        /// </summary>
        /// <param name="act"></param>
        private static async void SelectActivity(string act)
        {

            //change to controller input
            _controller.SetButtonState(Xbox360Button.RightThumb, true);
            Task.Delay(101, OrbitToken).Wait();
            _controller.SetButtonState(Xbox360Button.RightThumb, false);
            _controller.SetAxisValue(Xbox360Axis.LeftThumbY, STICK_BACK);
            Task.Delay(1000, OrbitToken).Wait();
            _controller.SetAxisValue(Xbox360Axis.LeftThumbY, STICK_CENTER);
            Task.Delay(500, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return;

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

                    _controller.SetButtonState(Xbox360Button.Down, true);
                    Task.Delay(101, OrbitToken).Wait();
                    _controller.SetButtonState(Xbox360Button.Down, false);
                    AwaitColorChange(50, 72, 1);
                    if (OrbitToken.IsCancellationRequested) return;
                    Task.Delay(1600, OrbitToken).Wait();
                    if (OrbitToken.IsCancellationRequested) return;
                }
            }
            if (DungeonActivityOrder.Contains(act))
            {
                screenloc = DungeonActivityOrder.IndexOf(act);
                _controller.SetButtonState(Xbox360Button.RightShoulder, true);
                Task.Delay(101, OrbitToken).Wait();
                _controller.SetButtonState(Xbox360Button.RightShoulder, false);
                AwaitColorChange(50, 72, 1);
                if (OrbitToken.IsCancellationRequested) return;
                Task.Delay(1500, OrbitToken).Wait();
                if (OrbitToken.IsCancellationRequested) return;

                if (DungeonActivityOrder.IndexOf(act) > 5)
                {
                    screenloc = DungeonActivityOrder.IndexOf(act) - 6;

                    _controller.SetButtonState(Xbox360Button.Down, true);
                    Task.Delay(101, OrbitToken).Wait();
                    _controller.SetButtonState(Xbox360Button.Down, false);
                    AwaitColorChange(50, 72, 1);
                    if (OrbitToken.IsCancellationRequested) return;
                    Task.Delay(1600, OrbitToken).Wait();
                    if (OrbitToken.IsCancellationRequested) return;
                }
            }

            if (PantheonActivityOrder.Contains(act))
            {
                screenloc = PantheonActivityOrder.IndexOf(act) + 3;
                _controller.SetButtonState(Xbox360Button.RightShoulder, true);
                Task.Delay(101, OrbitToken).Wait();
                _controller.SetButtonState(Xbox360Button.RightShoulder, false);
                AwaitColorChange(50, 72, 1);
                if (OrbitToken.IsCancellationRequested) return;
                Task.Delay(300, OrbitToken).Wait();
                if (OrbitToken.IsCancellationRequested) return;
                _controller.SetButtonState(Xbox360Button.RightShoulder, true);
                Task.Delay(101, OrbitToken).Wait();
                _controller.SetButtonState(Xbox360Button.RightShoulder, false);
                AwaitColorChange(50, 72, 1);
                if (OrbitToken.IsCancellationRequested) return;
                Task.Delay(1500, OrbitToken).Wait();
                if (OrbitToken.IsCancellationRequested) return;
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
            Task.Delay(100, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return;

            SetCursorPos(selectpos.X, selectpos.Y);
            Task.Delay(100, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return;
            SendClick(selectpos);
            SetCursorPos(ConvertAspectRatioCoords(50, 50).X, ConvertAspectRatioCoords(50, 50).Y);
            AwaitColorChange(85.9375, 83.33333333, 1);
            Task.Delay(1000, OrbitToken).Wait();
        }

        /// <summary>
        /// assumes you're already on the activity that has the checkpoint icon.
        /// </summary>
        public static void removecheckpoint()
        {
            //highlight over the play button just to have a consistent location for the checkpoint on screen.
            SetCursorPos(ConvertAspectRatioCoords(75.117, 83.75).X, ConvertAspectRatioCoords(75.117, 83.75).Y);

            Task.Delay(200, OrbitToken).Wait();

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
                Task.Delay(101, OrbitToken).Wait();
                SendClick(cursorpos);
                Task.Delay(101, OrbitToken).Wait();
                SetCursorPos(cursorpos.X, cursorpos.Y);
                Task.Delay(101, OrbitToken).Wait();
                SendClick(cursorpos);
                Task.Delay(101, OrbitToken).Wait();

                _controller.SetButtonState(Xbox360Button.LeftThumb, true);
                Task.Delay(101, OrbitToken).Wait();
                _controller.SetButtonState(Xbox360Button.LeftThumb, true);
                Task.Delay(500, OrbitToken).Wait();

                _controller.SetButtonState(Xbox360Button.X, true);
                Task.Delay(3000, OrbitToken).Wait();
                _controller.SetButtonState(Xbox360Button.X, false);
                Task.Delay(101, OrbitToken).Wait();
            }

            _controller.SetButtonState(Xbox360Button.LeftThumb, true);
            Task.Delay(101, OrbitToken).Wait();
            _controller.SetButtonState(Xbox360Button.LeftThumb, false);
            Task.Delay(1000, OrbitToken).Wait();
        }

        /// <summary>
        /// assumes you're already on the activity page
        /// </summary>
        private static async void SelectMaster()
        {
            statussubtext = "Swapping to master difficulty...";
            UpdateTextDisplay();

            Point selectpos = ConvertAspectRatioCoords(86.29, 77.5694);
            SetCursorPos(selectpos.X, selectpos.Y);
            Task.Delay(101, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return;
            SendClick(selectpos);
            AwaitColorChange(85.9375, 83.33333333, 1);
            if (OrbitToken.IsCancellationRequested) return;
            
            selectpos = ConvertAspectRatioCoords(13.6719, 29.16666667);
            SetCursorPos(selectpos.X, selectpos.Y);
            Task.Delay(2000, OrbitToken).Wait();
            AwaitColorChange(39.9609375, 49.23611111111, 1);
            Task.Delay(1000,OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return;
            SendClick(selectpos);
            Task.Delay(1000, OrbitToken).Wait();

            _controller.SetButtonState(Xbox360Button.B, true);
            Task.Delay(101, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return;
            _controller.SetButtonState(Xbox360Button.B, false);
            Task.Delay(101, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return;
            _controller.SetButtonState(Xbox360Button.B, true);
            Task.Delay(101, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return;
            _controller.SetButtonState(Xbox360Button.B, false);

            AwaitColorChange(85.9375, 83.33333333, 1);
            SendClick(selectpos);
            Task.Delay(1000, OrbitToken).Wait();
        }

        /// <summary>
        /// assumes you're already on the activity page
        /// </summary>
        private static async void SelectFeats(List<string> feats)
        {
            statussubtext = "Selecting feats...";
            UpdateTextDisplay();

            Point selectpos = ConvertAspectRatioCoords(86.29, 77.5694);
            SetCursorPos(selectpos.X, selectpos.Y);
            Task.Delay(101, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return;
            SendClick(selectpos);
            AwaitColorChange(33.7890625, 25.8333333, 1);
            if (OrbitToken.IsCancellationRequested) return;

            selectpos = ConvertAspectRatioCoords(33.7890625, 25.8333333);
            SetCursorPos(selectpos.X, selectpos.Y);
            Task.Delay(100, OrbitToken).Wait();
            AwaitColorChange(33.7109375, 37.0833333, 1);
            Task.Delay(1000, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return;

            int i = 0;
            double gap = 5.2;
            foreach (string f in feats)
            {
                //"token","phase","battalions","challenges","cutthroat"
                double featslot = 0;
                if (f == "token") featslot = gap;
                if (f == "phase") featslot = gap*2;
                if (f == "battalions") featslot = gap * 3;
                if (f == "challenges") featslot = gap * 4;
                if (f == "cutthroat") featslot = gap * 5;

                selectpos = ConvertAspectRatioCoords(33.7890625 + (gap * i), 25.8333333);
                SetCursorPos(selectpos.X, selectpos.Y);
                Task.Delay(200, OrbitToken).Wait();
                selectpos = ConvertAspectRatioCoords(33.7890625 + featslot, 36.52777777);
                SetCursorPos(selectpos.X, selectpos.Y);
                Task.Delay(200, OrbitToken).Wait();
                SendClick(selectpos);
                Task.Delay(200, OrbitToken).Wait();
                i++;
            }

            Task.Delay(1000, OrbitToken).Wait();

            _controller.SetButtonState(Xbox360Button.B, true);
            Task.Delay(101, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return;
            _controller.SetButtonState(Xbox360Button.B, false);
            Task.Delay(101, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return;
            _controller.SetButtonState(Xbox360Button.B, true);
            Task.Delay(101, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return;
            _controller.SetButtonState(Xbox360Button.B, false);

            AwaitColorChange(85.9375, 83.33333333, 1);
            SendClick(selectpos);
            Task.Delay(1000, OrbitToken).Wait();
        }

        /// <summary>
        /// assumes you're already on the activity page
        /// </summary>
        private static async void ClearFeats()
        {
            statussubtext = "Selecting feats...";
            UpdateTextDisplay();

            Point selectpos = ConvertAspectRatioCoords(86.29, 77.5694);
            SetCursorPos(selectpos.X, selectpos.Y);
            Task.Delay(101, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return;
            SendClick(selectpos);
            AwaitColorChange(33.7890625, 25.8333333, 1);
            if (OrbitToken.IsCancellationRequested) return;

            selectpos = ConvertAspectRatioCoords(33.7890625, 25.8333333);
            SetCursorPos(selectpos.X, selectpos.Y);
            Task.Delay(100, OrbitToken).Wait();
            AwaitColorChange(33.7109375, 37.0833333, 1);
            Task.Delay(1000, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return;

            double gap = 5.2;
            for(int i = 0; i < 5; i++)
            {
                //"token","phase","battalions","challenges","cutthroat"

                selectpos = ConvertAspectRatioCoords(33.7890625 + (gap * i), 25.8333333);
                SetCursorPos(selectpos.X, selectpos.Y);
                Task.Delay(200, OrbitToken).Wait();
                selectpos = ConvertAspectRatioCoords(33.7890625, 36.52777777);
                SetCursorPos(selectpos.X, selectpos.Y);
                Task.Delay(200, OrbitToken).Wait();
                SendClick(selectpos);
                Task.Delay(200, OrbitToken).Wait();
            }

            Task.Delay(1000, OrbitToken).Wait();

            _controller.SetButtonState(Xbox360Button.B, true);
            Task.Delay(101, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return;
            _controller.SetButtonState(Xbox360Button.B, false);
            Task.Delay(101, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return;
            _controller.SetButtonState(Xbox360Button.B, true);
            Task.Delay(101, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return;
            _controller.SetButtonState(Xbox360Button.B, false);

            AwaitColorChange(85.9375, 83.33333333, 1);
            SendClick(selectpos);
            Task.Delay(1000, OrbitToken).Wait();
        }

        private static async void InvitePlayer(string chat)
        {
            statussubtext = "Invitingn to fireteam...";
            UpdateTextDisplay();

            Task.Delay(1000, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return;

            InputSimulator sim = new InputSimulator();
            sim.Keyboard.KeyPress(VirtualKeyCode.BACK);
            Task.Delay(101, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return;
            sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            Task.Delay(1000, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return;
            sim.Keyboard.TextEntry(chat);
            Task.Delay(1000, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return;
            sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
        }

        private static bool JoinFireteamFromOrbit(string chat)
        {
            statussubtext = "Typing in fireteam name...";
            UpdateTextDisplay();

            Task.Delay(1000, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return false;

            InputSimulator sim = new InputSimulator();
            sim.Keyboard.KeyPress(VirtualKeyCode.BACK);
            Task.Delay(101, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return false;
            sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            Task.Delay(1000, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return false;
            sim.Keyboard.TextEntry(chat);
            Task.Delay(1000, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return false;
            sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

            statussubtext = "Checking if theres an error code...";
            UpdateTextDisplay();

            Task.Delay(5000, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return false;

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

            Task.Delay(1000, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return false;

            string ocrstring = GetText(bmpScreenshot).ToLower().Replace("(", "").Replace(")", "").Replace("'", "").Replace(":", "").Replace("\n", "");
            bmpScreenshot.Dispose();


            Task.Delay(1000, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return false;

            if (ocrstring.ToLower().Contains("unable") || ocrstring == "")
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

                while (GetColorAt(ConvertAspectRatioCoords(50, 50)) != black) //wait until on next black screen
                {
                    if (OrbitToken.IsCancellationRequested) return false;
                }

                AwaitColorChange(3, 3, 3); //come out of black screen

                statussubtext = "Join successful. Waiting for second black screen...";
                UpdateTextDisplay();

                while (GetColorAt(ConvertAspectRatioCoords(50, 50)) != black) //wait until on next black screen
                {
                    if (OrbitToken.IsCancellationRequested) return false;
                }

                AwaitColorChange(90, 90, 3); //come out of black screen, boots on ground
                Task.Delay(1000, OrbitToken).Wait();

                statussubtext = "Join successful. Now boots on ground...";
                UpdateTextDisplay();

                bootsonground = true;

                return true;
            }
        }

        private static bool JoinFireteamInOrbit(string chat)
        {
            statussubtext = "Typing in fireteam name...";
            UpdateTextDisplay();

            Task.Delay(1000, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return false;

            InputSimulator sim = new InputSimulator();
            sim.Keyboard.KeyPress(VirtualKeyCode.BACK);
            Task.Delay(101, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return false;
            sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            Task.Delay(1000, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return false;
            sim.Keyboard.TextEntry(chat);
            Task.Delay(1000, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return false;
            sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

            statussubtext = "Checking if theres an error code...";
            UpdateTextDisplay();

            Task.Delay(5000, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return false;

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

            Task.Delay(1000, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return false;

            string ocrstring = GetText(bmpScreenshot).ToLower().Replace("(", "").Replace(")", "").Replace("'", "").Replace(":", "").Replace("\n", "");
            bmpScreenshot.Dispose();

            Task.Delay(1000, OrbitToken).Wait();
            if (OrbitToken.IsCancellationRequested) return false;

            if (ocrstring.ToLower().Contains("unable") || ocrstring == "")
            {
                statussubtext = "Error code detected :(";
                UpdateTextDisplay();
                return false;
            }
            else
            {
                if (OrbitToken.IsCancellationRequested) return false;
                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                Task.Delay(1000, OrbitToken).Wait();
                if (OrbitToken.IsCancellationRequested) return false;
                sim.Keyboard.TextEntry("You may now launch the map, and get prepared to change characters.");
                Task.Delay(1000, OrbitToken).Wait();
                if (OrbitToken.IsCancellationRequested) return false;
                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                if (OrbitToken.IsCancellationRequested) return false;
                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                Task.Delay(1000, OrbitToken).Wait();
                if (OrbitToken.IsCancellationRequested) return false;
                sim.Keyboard.TextEntry("I will tell you when to hit confirm on changing characters. Please navigate to the \"confirm\" screen.");
                Task.Delay(1000, OrbitToken).Wait();
                if (OrbitToken.IsCancellationRequested) return false;
                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                //joining
                statussubtext = "Join successful. Detecting first black screen...";
                UpdateTextDisplay();

                if (OrbitToken.IsCancellationRequested) return false;
                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                while (GetColorAt(ConvertAspectRatioCoords(55, 55)) != black) //wait until on next black screen
                {
                    if (OrbitToken.IsCancellationRequested) return false;
                    Task.Delay(101, OrbitToken).Wait();
                }

                //send notice message
                if (OrbitToken.IsCancellationRequested) return false;
                sim.Keyboard.TextEntry("Please change characters now.");
                Task.Delay(101, OrbitToken).Wait();
                if (OrbitToken.IsCancellationRequested) return false;
                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                return true;
            }
        }

        private static void CleanCheckpoints()
        {

            deletingcheckpoint = true;

            List<string> order = new List<string>();
            order.AddRange(RaidActivityOrder);
            order.AddRange(DungeonActivityOrder);
            order.AddRange(PantheonActivityOrder);

            foreach (string activity in checkpoints.Keys)
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

                foreach (string activity in checkpoints.Keys)
                {
                    foreach (int output in checkpoints[activity].Values)
                    {
                        if (output == i)
                        {
                            activ.Remove(activity);
                        }
                        if (OrbitToken.IsCancellationRequested) return;
                    }
                }

                foreach (string activity in activ)
                {
                    statussubtext = "Clearing out " + activity;
                    UpdateTextDisplay();
                    if (initializing) UpdateStatusBar("Initializing... Cleaning up character slot " + i + " on activity " + activ.IndexOf(activity) + "/" + activ.Count(), UserStatusType.DoNotDisturb);
                    if (!initializing) UpdateStatusBar("Cleaning up character slot " + i + " on activity " + activ.IndexOf(activity) + "/" + activ.Count(), UserStatusType.Idle);

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
                        if(page < 2)
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
                    if (OrbitToken.IsCancellationRequested) return;
                    if(activity.Contains("master")) SelectMaster();
                    SendClick(new Point(50, 50));

                    if (OrbitToken.IsCancellationRequested) return;

                    removecheckpoint();
                    Task.Delay(101, OrbitToken).Wait();

                    _controller.SetButtonState(Xbox360Button.B, true);
                    Task.Delay(101, OrbitToken).Wait();
                    _controller.SetButtonState(Xbox360Button.B, false);
                    Task.Delay(101, OrbitToken).Wait();
                    if (OrbitToken.IsCancellationRequested) return;
                }

                _controller.SetButtonState(Xbox360Button.B, true);
                Task.Delay(101, OrbitToken).Wait();
                _controller.SetButtonState(Xbox360Button.B, false);
                Task.Delay(101, OrbitToken).Wait();
                if (OrbitToken.IsCancellationRequested) return;

                _controller.SetButtonState(Xbox360Button.B, true);
                Task.Delay(101, OrbitToken).Wait();
                _controller.SetButtonState(Xbox360Button.B, false);
                if (OrbitToken.IsCancellationRequested) return;

                _controller.SetButtonState(Xbox360Button.B, true);
                Task.Delay(101, OrbitToken).Wait();
                _controller.SetButtonState(Xbox360Button.B, false);
                Task.Delay(101, OrbitToken).Wait();
                if (OrbitToken.IsCancellationRequested) return;

                _controller.SetButtonState(Xbox360Button.B, true);
                Task.Delay(101, OrbitToken).Wait();
                _controller.SetButtonState(Xbox360Button.B, false);
                if (OrbitToken.IsCancellationRequested) return;

                ReturnToCharSelect();
            }
        }

        public static void awaittext(string inputtext, Point coordinatepointstart, Point coordinatepointend)
        {
            string currenttext = "";
            bool match = false;
            while (StringDifference(inputtext, currenttext) < .9)
            {
                if (OrbitToken.IsCancellationRequested) return;
                int width = d2window.Right - d2window.Left;
                int height = d2window.Bottom - d2window.Top;
                int iconwidth = coordinatepointend.X - coordinatepointstart.X;
                int iconheight = coordinatepointend.Y - coordinatepointstart.Y;
                int xpos = coordinatepointstart.X;
                int ypos = coordinatepointstart.Y;

                Bitmap bmpScreenshot = new Bitmap(iconwidth, iconheight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Graphics g = Graphics.FromImage(bmpScreenshot);
                Task.Delay(500, OrbitToken).Wait();

                g.CopyFromScreen(xpos, ypos, 0, 0, new System.Drawing.Size(iconwidth, iconheight));
                Task.Delay(500, OrbitToken).Wait();

                bmpScreenshot.ApplyEffect(new System.Drawing.Imaging.Effects.GrayScaleEffect());
                bmpScreenshot.ApplyEffect(new System.Drawing.Imaging.Effects.BrightnessContrastEffect(-25, 0));
                bmpScreenshot.ApplyEffect(new System.Drawing.Imaging.Effects.BrightnessContrastEffect(0, 100));
                bmpScreenshot.ApplyEffect(new System.Drawing.Imaging.Effects.BlurEffect(2, false));
                bmpScreenshot.ApplyEffect(new System.Drawing.Imaging.Effects.InvertEffect());

                Task.Delay(500, OrbitToken).Wait();
                if (OrbitToken.IsCancellationRequested) return;

                string ocrstring = GetText(bmpScreenshot).ToLower().Replace("(", "").Replace(")", "").Replace("'", "").Replace(":", "").Replace("\n", "");
                bmpScreenshot.Dispose();

                Task.Delay(1000, OrbitToken).Wait();
                if (OrbitToken.IsCancellationRequested) return;
                currenttext = ocrstring;
            }
        }

        public static void savecheckpoints()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string output = "";

            foreach (string activityname in checkpoints.Keys)
            {
                output = output + activityname;
                if (checkpoints[activityname].Count > 0)
                {
                    foreach (string checkpointname in checkpoints[activityname].Keys)
                    {
                        output = output + "~" + checkpointname + "." + checkpoints[activityname][checkpointname];
                    }
                }
                output = output + "-";
            }

            output = output.TrimEnd('-');


            if (File.Exists(path + "\\checkpoints.ini"))
            {
                File.Delete(path + "\\checkpoints.ini");
            }
            File.WriteAllText(path + "\\checkpoints.ini", output);
        }

        public static bool checkreset()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

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

            DateTime now = DateTime.Now.ToUniversalTime();

            //compare intended reset to now.
            if (temp > now)
            {
                //reset didnt happen yet.
                return false;
            }
            else
            {
                //reset has happened.

                return true;
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
            for(int i = 0; i < length; i++)
            {
                if (c1[i] == c2[i]) same++;
            }

            double match = same / length;

            return match;
        }

    }
}