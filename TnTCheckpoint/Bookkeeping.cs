using static TnTCheckpoint.ConstantsAndGlobals;
using System.IO;

namespace TnTCheckpoint
{
    public class Bookkeeping
    {

        public static void SaveCheckpoints()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string output = "";

            foreach (string activityname in Checkpoints.Keys)
            {
                output = output + activityname;
                if (Checkpoints[activityname].Count > 0)
                {
                    foreach (string checkpointname in Checkpoints[activityname].Keys)
                    {
                        output = output + "~" + checkpointname + "." + Checkpoints[activityname][checkpointname];
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

        public static bool CheckReset()
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

        public static void KillProcess()
        {
            D2Process.Kill();
            Environment.Exit(0);
        }
    }
}
