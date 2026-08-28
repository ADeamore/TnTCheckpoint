using NetCord;
using NetCord.Gateway;
using static TnTCheckpoint.ConstantsAndGlobals;

namespace TnTCheckpoint
{
    public class DebugCommunication
    {
        public static void UpdateTextDisplay()
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
            await DiscordClient.UpdatePresenceAsync(
                new PresenceProperties(stat).WithActivities([new UserActivityProperties("ignored", UserActivityType.Custom).WithState(status)])
            );
        }
    }
}
