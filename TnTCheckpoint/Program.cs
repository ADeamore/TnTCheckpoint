using System.Diagnostics;
using System.IO;
using System.Reflection;
using NetCord;
using static TnTCheckpoint.DLLImportsStructsAndEnums;
using static TnTCheckpoint.ConstantsAndGlobals;
using static TnTCheckpoint.DebugCommunication;
using static TnTCheckpoint.StartupAndInitialization;
using static TnTCheckpoint.Macros;
using static TnTCheckpoint.Bookkeeping;

namespace TnTCheckpoint
{
    class TnTCheckpoint
    {
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
                        DiscordDevToken = resp;
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
                            ulong temp = 0;
                            try
                            {
                                temp = ulong.Parse(resp);
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
                                DiscordChannelID = temp;

                                File.WriteAllText(path + "\\configuration.ini", DiscordDevToken + "\n" + temp);
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

                    if (resp.ToLower() == "launch")
                    {
                        string[] strings = File.ReadAllLines(path + "\\configuration.ini");
                        if (strings.Length != 2)
                        {
                            File.Delete(path + "\\configuration.ini");
                            continue;
                        }
                        DiscordDevToken = strings[0];
                        ulong temp = 0;
                        try
                        {
                            temp = ulong.Parse(strings[1]);
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
                            DiscordChannelID = temp;
                        }
                        else
                        {
                            //channel id fails. we bail and restart.
                            File.Delete(path + "\\configuration.ini");
                            continue;
                        }
                    }
                    if (resp.ToLower() == "reconfig")
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
                                if (STARTUP)
                                {
                                    STARTUP = false;
                                    statusheader = "Launch Conditions:";
                                    statussubtext = "Launching Destiny...";
                                    UpdateTextDisplay();
                                    string strCmdText = "/C start steam://rungameid/1085660";
                                    Process.Start("CMD.exe", strCmdText).WaitForExit();
                                }
                                if (!flagIntroSection)
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
                                if (STARTUP)
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
                                                if (flagIntroSection)
                                                {
                                                    statusheader = "Launch Conditions:";
                                                    statussubtext = "Game window found, continuing initialization.";
                                                    UpdateTextDisplay();
                                                    PrepCharMenu(); 

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

                            Thread.Sleep(500);
                        }
                    }).Start();
                }
            }).Start();

            new Thread(async () =>
            {
                Thread.CurrentThread.IsBackground = true;
                //close button stuff.
                string oldstatus = "";
                FlagAFKTimer = DateTime.Now.AddMinutes(55);

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

                    if (!INITIALIZING)
                    {
                        if (D2Process == null)
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
                        else
                        {
                            //reset stuff
                            if (DateTime.Now > D2RESETTIME)
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

                            //menu afk cycle
                            if (flagOnCharSelect)
                            {
                                //currently running afk timer
                                if (DateTime.Now > FlagAFKTimer)
                                {
                                    FlagAFKTimer = DateTime.Now.AddMinutes(55);
                                    new Thread(() =>
                                    {
                                        UpdateStatusBar("AFK cycle", UserStatusType.DoNotDisturb);
                                        AFKCYCLE = true;
                                        SelectChar(1);
                                        Task.Delay(5000).Wait();
                                        ReturnToCharSelectFast();
                                        AFKCYCLE = false;
                                        UpdateStatusBar("Idle...", UserStatusType.Online);
                                    }).Start();
                                }
                            }
                            else
                            {
                                //push forward afk timer so once im done doing things it resumes with generous leeway.
                                FlagAFKTimer = DateTime.Now.AddMinutes(55);
                            }

                            //close button stuff
                            if (!CloseButtonPressed)
                            {
                                if (Keyboard.IsPressed(0xA5))
                                {
                                    oldstatus = statussubtext;
                                    CloseButtonPressed = true;
                                    CloseButtonKillTime = DateTime.Now.AddSeconds(5);
                                }
                            }
                            else
                            {
                                if (!Keyboard.IsPressed(0xA5))
                                {
                                    statussubtext = oldstatus;
                                    UpdateTextDisplay();
                                    CloseButtonPressed = false;
                                    CloseButtonKillTime = DateTime.MaxValue;
                                }
                                else
                                {
                                    statussubtext = "Killing process... " + Math.Ceiling((CloseButtonKillTime - DateTime.Now).TotalSeconds);
                                    UpdateTextDisplay();
                                    if (DateTime.Now > CloseButtonKillTime)
                                    {
                                        DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Kill command recieved from host computer. Going offline... :(").Wait();
                                        KillProcess();
                                    }
                                }
                            }
                        }
                    }
                    Thread.Sleep(500);
                }
            }).Start();

            Thread.Sleep(Timeout.Infinite);
        }
    }
}