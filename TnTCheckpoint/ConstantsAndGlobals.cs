using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using NetCord.Gateway;
using static TnTCheckpoint.DLLImportsStructsAndEnums;

namespace TnTCheckpoint
{
    public class ConstantsAndGlobals
    {
        //controller positions
        public const short STICK_CENTER = 0;
        public const short STICK_BACK = short.MinValue;
        public const short STICK_FORWARD = short.MaxValue;
        public const short STICK_LEFT = short.MinValue;
        public const short STICK_RIGHT = short.MaxValue;
        public const byte TRIGGER_PULLED = byte.MaxValue;
        public const byte TRIGGER_RELEASED = byte.MinValue;

        //empty string cuz its easier than the alternative
        public const string emptybar = "                                                                                                                                          ";

        //D2 process handling
        public static RECT d2window = new RECT();
        public static Process D2Process;
        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
        public static bool STARTUP = true;

        //controller/keyboard input handling
        public static ViGEmClient? ControllerClient;
        public static IXbox360Controller? Controller;
        public static bool _connected = false;

        //macro program modes and flags
        public static bool HOLDINGLOAD = false;
        public static bool FARMMODE = false;
        public static bool AFKCYCLE = false;
        public static bool GRABBINGCHECKPOINT = false;
        public static bool CLEANINGCHECKPOINTS = false;
        public static bool VERIFYING = false;
        public static int VERIFYINGLEVEL = 0;
        public static bool TRANSFERINGCHECKPOINT = false;
        public static bool DELETINGCHECKPOINT = false;
        public static bool INITIALIZING = true;
        public static DateTime D2RESETTIME = DateTime.MaxValue;
        public static bool flagBootsOnGround = false;
        public static bool flagOnCharSelect = false;
        public static bool flagIntroSection = true;
        public static bool FlagGotActivityOrder = false;
        public static DateTime FlagAFKTimer = DateTime.Now.AddMinutes(55);

        //discord bot handling
        public static GatewayClient DiscordClient;
        public static string DiscordDevToken = "";
        public static ulong DiscordChannelID = 0;

        //communication with moderators and with discord server
        public static string statusheader = "";
        public static string statussubtext = "";
        public static int visualupdates = 0;
        public static int visualupdatestotal = 0;
        public static int visualupdatesx = 0;
        public static int visualupdatesy = 0;
        public static bool runningupdatedetection = false;

        //recordkeeping for ingame names/activities/etc
        public static List<string> RaidActivityOrder = new List<string>();
        public static List<string> DungeonActivityOrder = new List<string>();
        public static List<string> PantheonActivityOrder = new List<string>();
        public static Dictionary<string, Dictionary<string, int>> Checkpoints = new Dictionary<string, Dictionary<string, int>>();
        public static string WorkingUserName = "";
        public static string WorkingDiscordName = "";
        public static string WorkingActivityName = "";
        public static string WorkingCheckpointName = "";

        //administrative
        public static DateTime CloseButtonKillTime = DateTime.MaxValue;
        public static bool CloseButtonPressed = false;
    }
}
