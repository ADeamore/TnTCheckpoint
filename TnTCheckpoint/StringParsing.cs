using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using NetCord;
using NetCord.Gateway;
using static TnTCheckpoint.Bookkeeping;
using static TnTCheckpoint.ConstantsAndGlobals;
using static TnTCheckpoint.DebugCommunication;
using static TnTCheckpoint.DLLImportsStructsAndEnums;
using static TnTCheckpoint.Macros;
using static TnTCheckpoint.StartupAndInitialization;

namespace TnTCheckpoint
{
    public class StringParsing
    {
        public static bool IsBusyWithOtherCommand()
        {

            if (!flagOnCharSelect)
            {
                if (TRANSFERINGCHECKPOINT)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm transferring a checkpoint for " + WorkingDiscordName + "\n" + "If I'm mistaken in this please run either \"!endhold\" or \"!forceorbit\" depending on how mistaken I am.");
                    return true;
                }
                if (HOLDINGLOAD)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm currently holding a load for " + WorkingDiscordName + "\n" + "If I'm mistaken in this please run either \"!endhold\" or \"!forceorbit\" depending on how mistaken I am.");
                    return true;
                }
                if (FARMMODE)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm currently helping " + WorkingDiscordName + " farm " + WorkingActivityName + " - " + WorkingCheckpointName + "\nIf I'm mistaken in this please run either \"!endfarm\" or \"!forceorbit\" depending on how mistaken I am.");
                    return true;
                }
                if (GRABBINGCHECKPOINT)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm currently grabbing a checkpoint from " + WorkingDiscordName + "\nIf I'm mistaken in this please run \"!forceorbit\" to help me find my bearings.");
                    return true;
                }
                if (DELETINGCHECKPOINT)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm currently deleting a checkpoint. Please wait a moment.\nIf I'm mistaken in this please run \"!forceorbit\" to help me find my bearings.");
                    return true;
                }
                if (CLEANINGCHECKPOINTS)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm currently cleaning up my saved checkpoints. Please wait a moment.\nIf I'm mistaken in this please run \"!forceorbit\" to help me find my bearings.");
                    return true;
                }
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I don't believe I'm in orbit right now.\nIf this is a mistake, please run \"!forceorbit\" for me to rectify the situation.");
                return true;
            }
            return false;
        }

        public static bool DoubleCheckAndHandleReset()
        {
            if (checkreset())
            {
                INITIALIZING = true;
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Looks like I no longer have that checkpoint due to reset and need to clean everything up. Sorry for the inconvenience. I'm gonna be down for the next 30 minutes or so while I work this out.");
                InitializeCheckpoints();
                GetToDirectorForActivityCoords();
                CleanCheckpoints();
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm now done cleaning up post-reset, and I'm funcitonal again now.");

                INITIALIZING = false;
                return true;
            }
            return false;
        }

        public static bool UsernameHashtagValid(string commandname,string[] nameID)
        {

            if (nameID.Length == 1 || nameID.Last().Split('#').Last().Length != 4)
            {
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "For the \"" + commandname + "\" command to work, I need the 4 number hashtag after your guardians name.\nTry \"!help " + commandname + "\" to learn how to use it.");

                statusheader = "Idle...";
                statussubtext = "";
                UpdateTextDisplay();

                UpdateStatusBar("Idle...", UserStatusType.Online);
                return false;
            }
            return true;
        }

        public static bool CheckActivityExists(string commandname, string activity)
        {
            if (!Checkpoints.Keys.ToArray().Contains(activity))
            {
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I appear to not know what activity " + activity + " is.\nTry \"!help " + commandname + "\" to learn how to use this command, or \"!activities\" to see what activities are available.");
                
                statusheader = "Idle...";
                statussubtext = "";
                UpdateTextDisplay();

                UpdateStatusBar("Idle...", UserStatusType.Online);
                return false;
            }

            return true;
        }

        public static bool VerifyCommandLength(string commandname, int length, int desiredlength)
        {
            if (length < desiredlength)
            {
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Command length too short.\nTry \"!help " + commandname + "\" to learn how to use it.");
                
                statusheader = "Idle...";
                statussubtext = "";
                UpdateTextDisplay();

                UpdateStatusBar("Idle...", UserStatusType.Online);
                return false;
            }
            return true;
        }

        /// <summary>
        /// return[0] == if master
        /// return[1] == if master mode exists
        /// </summary>
        public static bool[] CheckMasterValue(string m, string activity)
        {
            bool[] retbool = new bool[2];
            retbool[0] = false; //is master mode
            retbool[1] = true; //is command valid


            if (m == "master" || m == "m")
            {
                retbool[0] = true;
                if (!Checkpoints.Keys.ToArray().Contains("master" + activity))
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "That activity doesn't appear to have a master mode. Sorry.");
                    
                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    retbool[1] = false;
                }
            }
            return retbool;
        }

        public static string GetNameStringForCommand(string[] command, int start)
        {
            string res = "";

            for (int i = start; i < command.Length; i++)
            {
                if (res == "") res = res + command[i];
                else res = res + " " + command[i];
            }

            return res;
        }

        public static string CleanCheckpointName(string checkpointName)
        {
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            return rgx.Replace(checkpointName, "");
        }


        /// <summary>
        /// return[0] == if feats
        /// return[1] == if feats mode exists
        /// </summary>
        public static bool[] CheckIfFeats(string inputchunk,string activitykey)
        {
            bool[] retbool = new bool[2];
            retbool[0] = false;
            retbool[1] = true;
            if (inputchunk.ToLower().Split(":")[0] == "feats" & inputchunk.ToLower().Split(":").Length > 1)
            {
                retbool[0] = true;
                if (activitykey != "DP" & activitykey != "EDP" & activitykey != "EQ")
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "The activity you've selected does not support feats. Possible activities with feats are: \nDP, EDP, and EQ.");

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();
                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    retbool[1] = false;
                }
                else
                {
                    retbool[1] = true;
                }
            }
            return retbool;
        }
        public static List<string> ParseFeats(string inputchunk)
        {

            string[] possiblefeats = { "token", "phase", "battalions", "challenges", "cutthroat" };

            List<string> featlist = new List<string>();
            bool feats = false;
                
            foreach (string feat in inputchunk.Split(":")[1].Split(","))
            {
                if (!possiblefeats.Contains(feat.ToLower()))
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "One or more of the feats you requested aren't in my system. Legal feats include: \nToken, Phase, Battalions, Challenges, and Cutthroat.");
                    
                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();
                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    featlist.Clear();
                    return featlist;
                }
                else
                {
                    featlist.Add(feat.ToLower());
                }
            }
            return featlist;
        }

        public static CommandLayout ParseStandardCommand(string command, string commandname, bool shouldhavename)
        {
            CommandLayout output = new();
            string[] messagechunks = command.Split(" ");

            if(shouldhavename) output.works = VerifyCommandLength(commandname, messagechunks.Length, 4);
            else output.works = VerifyCommandLength(commandname, messagechunks.Length, 3);

            if (!output.works) return output;

            if(shouldhavename) output.works = UsernameHashtagValid(commandname, messagechunks.Last().Split("#"));
            output.works = CheckActivityExists(commandname, messagechunks[1].ToUpper());
            if (!output.works) return output;

            output.activity = messagechunks[1].ToUpper();
            output.activitykey = output.activity; //this will change if master is true
            output.master = false;

            bool[] mastervals = CheckMasterValue(messagechunks[2], output.activity);

            output.works = mastervals[1];
            if(!output.works) return output;

            //parse master and feats

            output.master = mastervals[0];
            if (output.master)
            {
                output.activitykey = "master" + output.activity;
                output.checkpointname = CleanCheckpointName(messagechunks[3]);
                if(shouldhavename) output.guardianname = GetNameStringForCommand(messagechunks, 4);
            }
            else
            {
                //check if feats here
                bool[] featvalues = CheckIfFeats(messagechunks[2], output.activitykey);
                output.feats = featvalues[0];
                if (output.feats)
                {
                    //is feats
                    if (featvalues[1])
                    {
                        //feats are on a valid activity
                        output.featlist = ParseFeats(messagechunks[2]);
                        if(output.featlist.Count == 0)
                        {
                            //there was an invalid feat in the list so the string list returned empty
                            output.works = false;
                            return output;
                        } 
                        output.checkpointname = CleanCheckpointName(messagechunks[3]);
                        if (shouldhavename) output.guardianname = GetNameStringForCommand(messagechunks, 4);

                    }
                    else
                    {
                        output.works = false;
                        return output;
                    }

                }
                else
                {
                    output.checkpointname = CleanCheckpointName(messagechunks[2]);
                    if (shouldhavename) output.guardianname = GetNameStringForCommand(messagechunks, 3);
                }
            }

            if(output.checkpointname == "")
            {
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "The checkpoint name you gave me when turned into just letters and numbers ends up as nothing.\n" +
                    "checkpoints can __only__ be named with letters and numbers.");
                output.works = false;
                return output;
            }

            return output;
        }

        public static bool CheckOverlappingCheckpointName(string checkname, string activitykey, string commandname)
        {
            if (Checkpoints[activitykey].Keys.Contains(checkname))
            {
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I already have a checkpoint with the name " + checkname + " in my save data.\nTry \"!help "+ commandname + "\" to learn how to use this command.");
                
                statusheader = "Idle...";
                statussubtext = "";
                UpdateTextDisplay();

                UpdateStatusBar("Idle...", UserStatusType.Online);
                return false;
            }
            return true;
        }

        public static bool CheckCheckpointsFull(string activitykey)
        {
            if (Checkpoints[activitykey].Keys.Count == 3)
            {
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I already have 3 checkpoints for that activity in my record.\nTry \"!help deletecheckpoint\" to learn how to delete a checkpoint so that you may overwrite it, and \"!listcheckpoints\" to see what checkpoints I have.");
                
                statusheader = "Idle...";
                statussubtext = "";
                UpdateTextDisplay();

                UpdateStatusBar("Idle...", UserStatusType.Online);
                return false;
            }
            return true;
        }

        public static int GetFirstEmptyCharSlot(string activitykey)
        {
            int charslot = 1;
            //charslot = checkpoints[activitykey].Count + 1;
            foreach (string tk in Checkpoints[activitykey].Keys)
            {
                if (Checkpoints[activitykey][tk] == 1) charslot = 2;
                if (Checkpoints[activitykey][tk] == 2) charslot = 3;
            }
            return charslot;
        }

        public static int GetCharSlotOfCheckpoint(string activitykey,string checkpointname)
        {
            if (Checkpoints[activitykey].Keys.Contains(checkpointname))
            {
                return Checkpoints[activitykey][checkpointname];
            }
            else
            {
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I don't currently have a checkpoint with the name " + checkpointname + " in my save data.\nTry \"!help DeleteCheckpoint\" to learn how to use this command, or \"!listcheckpoints\" to see what checkpoints I have.");
                
                statusheader = "Idle...";
                statussubtext = "";
                UpdateTextDisplay();

                UpdateStatusBar("Idle...", UserStatusType.Online);
                return 0;
            }
        }
    }

}
