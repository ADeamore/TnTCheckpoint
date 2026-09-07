using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using WindowsInput;
using WindowsInput.Native;
using static TnTCheckpoint.Bookkeeping;
using static TnTCheckpoint.ConstantsAndGlobals;
using static TnTCheckpoint.DebugCommunication;
using static TnTCheckpoint.DLLImportsStructsAndEnums;
using static TnTCheckpoint.Macros;
using static TnTCheckpoint.ScreenspaceInteractionsAndReading;
using static TnTCheckpoint.StartupAndInitialization;
using static TnTCheckpoint.StringParsing;
using Color = System.Drawing.Color;
using Message = NetCord.Gateway.Message;

namespace TnTCheckpoint
{
    public class CommandHandling
    {

        public static async ValueTask HandleMessages(Message message)
        {
            if (message.ChannelId != DiscordChannelID) return;
            if (message.Content == "") return;
            if (message.Content.First() != '!') return;
            if (!FlagGotActivityOrder || INITIALIZING)
            {
                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "I'm still getting set up, please be patient with me.\nTry your command again in a couple minutes.");
                return;
            }
            string[] words = message.Content.Split(' ');
            bool done = false;
            if (!VERIFYING) switch (words.First().ToLower())
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
                        CommandHoldLoad(message);
                        return;
                    case "!renamecheckpoint":
                        done = true;
                        RenameCheckpoint(message);
                        return;
                    case "!endhold":
                        done = true;
                        CommandEndHoldLoad(message);
                        return;
                    case "!grabcheckpoint":
                        done = true;
                        CommandGrabCheckpoint(message);
                        return;
                    case "!grabcheckpointandconfirm":
                        done = true;
                        CommandGrabCheckpointAndConfirm(message);
                        return;
                    case "!deletecheckpoint":
                        done = true;
                        CommandDeleteCheckpoint(message);
                        return;
                    case "!listcheckpoints":
                        done = true;
                        CommandListCheckpoint(message);
                        return;
                    case "!launchandhold":
                        done = true;
                        CommandLaunchAndHold(message);
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
                    case "!flyincheckpointtransfer":
                        done = true;
                        CommandFlyInCheckpointTransfer(message);
                        return;
                    case "!cleancheckpoints":
                        done = true;
                        CommandCleanCheckpoints(message);
                        return;
                    case "!listfeats":
                        done = true;
                        CommandListFeats(message);
                        return;
                    case "!gerbcheckpoint":
                        done = true;
                        CommandGerbCheckpoint(message);
                        return;
                    case "!forcerestart":
                        done = true;
                        CommandForceRestart(message);
                        return;
                }
            if (VERIFYING)
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
                        VERIFYINGLEVEL++;
                        return;
                    case "!v":
                        done = true;
                        VERIFYINGLEVEL++;
                        return;
                    case "!forcerestart":
                        done = true;
                        CommandForceRestart(message);
                        return;
                    case "!listfeats":
                        done = true;
                        CommandListFeats(message);
                        return;
                    case "!cancel":
                        VERIFYING = false;
                        VERIFYINGLEVEL = 0;
                        done = true;
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Verification cancelled.");
                        return;
                    case "!c":
                        VERIFYING = false;
                        VERIFYINGLEVEL = 0;
                        done = true;
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Verification cancelled.");
                        return;
                }
                if (!done)
                {
                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "I'm currently waiting to verify a previous command. please be patient.");
                    done = true;
                }
            }
            if (!done)
            {
                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Invalid command, run !listcommands to see all available commands.");
            }
        }

        public static async void CommandForceRestart(Message message)
        {
            //start a new thread so i can get more commands in the meantime. it doesnt matter here but it will in other places.

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                if (VERIFYING)
                {
                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Stopping previous verification. Gimme 5 seconds...");
                    VERIFYING = false;
                    Task.Delay(5000).Wait();
                }

                //verify.
                VERIFYING = true;
                VERIFYINGLEVEL = 0;

                string oldstatus = statusheader;
                string oldsubtext = statussubtext;

                statusheader = "Processing !ForceRestart command:";
                statussubtext = "Awaiting first verification.";
                UpdateTextDisplay();

                UpdateStatusBar("!ForceRestart: Verification step 1/2", UserStatusType.Idle);

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "I want to make sure we're on the same page. This will make me forget what I was doing and send me back to character select.\nSend \"!verify\" to confirm, or \"!cancel\" to cancel.\nIf no response is given in 60 seconds I will cancel on my own.");

                DateTime timeout = DateTime.Now.AddMinutes(1);
                while (VERIFYINGLEVEL == 0)
                {
                    if (!VERIFYING) return;
                    if (DateTime.Now > timeout)
                    {
                        VERIFYING = false;
                        VERIFYINGLEVEL = 0;
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId, "No valid response given in time. Continuing without returning to orbit.");

                        statusheader = oldstatus;
                        statussubtext = oldsubtext;
                        UpdateTextDisplay();
                        UpdateStatusBar("Idle...", UserStatusType.Online);
                        return;
                    }
                }
                //verify again.

                statusheader = "Processing !ForceRestart command:";
                statussubtext = "Awaiting second verification.";
                UpdateTextDisplay();

                UpdateStatusBar("!ForceRestart: Verification step 2/2", UserStatusType.Idle);

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "I'm double checking. \"!verify\" to verify again, \"!cancel\" to cancel.");
                timeout = DateTime.Now.AddMinutes(1);
                while (VERIFYINGLEVEL == 1)
                {
                    if (!VERIFYING) return;
                    if (DateTime.Now > timeout)
                    {
                        VERIFYING = false;
                        VERIFYINGLEVEL = 0;
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId, "No valid response given in time. Continuing without returning to orbit.");

                        statusheader = oldstatus;
                        statussubtext = oldsubtext;
                        UpdateTextDisplay();
                        UpdateStatusBar("Idle...", UserStatusType.Online);
                        return;
                    }
                }
                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Restarting everything. o7").Wait();

                VERIFYING = false;
                VERIFYINGLEVEL = 0;

                D2Process.Kill();

            }).Start();
        }

        public static async void CommandCleanCheckpoints(Message message)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                if (DoubleCheckAndHandleReset()) return;
                if (IsBusyWithOtherCommand()) return;

                statusheader = "!CleanCheckpoints command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                VERIFYING = true;
                VERIFYINGLEVEL = 0;

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "This command takes about 30 minutes to run. I want to make sure we're on the same page here. run \"!verify\" to proceed.");

                DateTime timeout = DateTime.Now.AddMinutes(1);
                while (VERIFYINGLEVEL == 0)
                {
                    if (!VERIFYING) return;
                    if (DateTime.Now > timeout)
                    {
                        VERIFYING = false;
                        VERIFYINGLEVEL = 0;
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId, "No valid response given in time. Continuing without returning to orbit.");
                    }
                }
                VERIFYING = false;
                CLEANINGCHECKPOINTS = true;

                WorkingDiscordName = message.Author.Username;
                if (message.Author.GlobalName != null) WorkingDiscordName = message.Author.GlobalName;

                while (AFKCYCLE)
                {
                }

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "I'm now cleaning checkpoints. Estimated finish time is 30 minutes after this message.");

                CleanCheckpoints();

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Cleaning checkpoints is done, I'm back to idling...");

                CLEANINGCHECKPOINTS = false;

                UpdateStatusBar("Idle...", UserStatusType.Online);

            }).Start();
        }

        public static async void CommandFlyInCheckpointTransfer(Message message)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                //make sure reset hasnt happened yet.
                if (DoubleCheckAndHandleReset()) return;
                if (IsBusyWithOtherCommand()) return;

                statusheader = "!FlyInCheckpointTransfer command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                UpdateStatusBar("!FlyInCheckpointTransfer... Making sure the command is viable.", UserStatusType.Idle);

                GRABBINGCHECKPOINT = true;

                WorkingDiscordName = message.Author.Username;
                if (message.Author.GlobalName != null) WorkingDiscordName = message.Author.GlobalName;

                while (AFKCYCLE)
                {
                    //stall till not afk
                }

                CommandLayout output = ParseStandardCommand(message.Content, "FlyInCheckpointTransfer", true); //this variable holds everything about the activity
                if (!output.works)
                {
                    GRABBINGCHECKPOINT = false;
                    return;
                }

                GRABBINGCHECKPOINT = CheckOverlappingCheckpointName(output.checkpointname, output.activitykey, "FlyInCheckpointTransfer");
                if (!GRABBINGCHECKPOINT) return;
                GRABBINGCHECKPOINT = CheckCheckpointsFull(output.activitykey);
                if (!GRABBINGCHECKPOINT) return;

                int charslot = GetFirstEmptyCharSlot(output.activitykey);
                WorkingUserName = output.guardianname;


                //BEGIN MACRO
                statusheader = "!FlyInCheckpointTransfer command:";
                statussubtext = "Attempting to join...";
                UpdateTextDisplay();

                UpdateStatusBar("!FlyInCheckpointTransfer: Attempting to join...", UserStatusType.Idle);

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Checking to see if I can join...");
                SelectChar(charslot);

                bool worked = JoinFireteamInOrbit("/join " + WorkingUserName);
                if (!worked)
                {
                    statussubtext = "Error code detected :(";
                    UpdateTextDisplay();

                    if (CheckIfOrbitTextBox() != "") //ERROR CODE
                    {
                        //make sure im in controller mode
                        Controller.SetButtonState(Xbox360Button.RightThumb, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, false);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, false);
                        Task.Delay(101).Wait();

                        Controller.SetButtonState(Xbox360Button.B, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.B, false);
                        Task.Delay(101).Wait();
                    }

                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Looks like your fireteam is currently unavailable. Returning to idling.");
                    GRABBINGCHECKPOINT = false;

                    ReturnToCharSelectFast();

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                }
                else
                {
                    InputSimulator sim = new InputSimulator();

                    //joining
                    statussubtext = "Join successful. Detecting first black screen...";
                    UpdateTextDisplay();

                    sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                    Task.Delay(1000).Wait();
                    sim.Keyboard.TextEntry("You may now launch the map, and get prepared to change characters.");
                    Task.Delay(1000).Wait();

                    sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                    Task.Delay(3000).Wait();

                    sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                    while (!CheckBlackScreen()) //wait until on next black screen
                    {
                        Task.Delay(20).Wait();
                    } //found black screen

                    //send notice message
                    sim.Keyboard.TextEntry("Please change characters now.");
                    Task.Delay(101).Wait();
                    sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                    while (CheckBlackScreen()) //wait for black screen to end, then return to char select
                    {
                        Task.Delay(20).Wait();
                    }

                    UpdateStatusBar("!FlyInCheckpointTransfer: Making sure the checkpoint saved correctly.", UserStatusType.Idle);

                    statusheader = "!FlyInCheckpointTransfer command:";
                    statussubtext = "Boots on ground. Returning to orbit.";
                    UpdateTextDisplay();

                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "I'm now double checking to make sure the checkpoint saved correctly.");

                    //return to char select, then see if the checkpoint saved.
                    ReturnToCharSelectFast();

                    VerifyCheckpointAndSave(charslot, output);

                    ReturnToCharSelectFast();

                    GRABBINGCHECKPOINT = false;
                    flagBootsOnGround = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);

                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "FlyInCheckpointTransfer complete. Back to idling...");
                }
            }).Start();
        }

        public static async void CommandTransferCheckpoint(Message message)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                //make sure reset hasnt happened yet.

                if (DoubleCheckAndHandleReset()) return;
                if (IsBusyWithOtherCommand()) return;

                statusheader = "!TransferCheckpoint command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                UpdateStatusBar("!TransferCheckpoint... Making sure the command is viable.", UserStatusType.Idle);

                TRANSFERINGCHECKPOINT = true;

                WorkingDiscordName = message.Author.Username;
                if (message.Author.GlobalName != null) WorkingDiscordName = message.Author.GlobalName;

                while (AFKCYCLE)
                {
                }

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Making sure I have the checkpoint and everything is correct...");

                //parse command text
                //!TransferCheckpoint [activity shorthand(!activities)] [(optional)master] [single word name given to the checkpoint. a-z, 1-9 only] BungieUsername#0000

                CommandLayout output = ParseStandardCommand(message.Content, "TransferCheckpoint", true);
                if (!output.works)
                {
                    TRANSFERINGCHECKPOINT = false;
                    return;
                }

                int charslot = GetCharSlotOfCheckpoint(output.activitykey, output.checkpointname);
                if (charslot == 0)
                {
                    TRANSFERINGCHECKPOINT = false;
                    return;
                }

                NavigateToActivityFromCharSelect(charslot, output.activity, output.master);

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Sending an invite to " + WorkingUserName + ". If I don't see someone join in the next 5 minutes I will return to idle mode.");
                InvitePlayer("/invite " + WorkingUserName);

                statussubtext = "Invite sent. Waiting for player to join.";
                UpdateTextDisplay();
                UpdateStatusBar("!TransferCheckpoint... Waiting for " + WorkingUserName + " to join.", UserStatusType.Idle);

                bool worked = WaitForPlayerJoinOrbit(DateTime.Now.AddMinutes(5));
                if (!worked)
                {
                    TRANSFERINGCHECKPOINT = false;
                    ReturnToCharSelectFast();
                    return;
                }

                UpdateStatusBar("!TransferCheckpoint... Detected join, Launching momentarily.", UserStatusType.Idle);
                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Launching activity, and then returning to orbit. Good luck on your run(s).");

                Task.Delay(2000).Wait();

                SpamLaunchButtonUntilWorks();

                Task.Delay(3000).Wait();

                DateTime bailout = DateTime.Now.AddSeconds(30);
                while (!CheckBlackScreen())
                {
                    if (DateTime.Now > bailout) break;
                    Task.Delay(30).Wait();
                }

                TRANSFERINGCHECKPOINT = false;

                ReturnToCharSelectFast();

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Idling...");

                UpdateStatusBar("Idle...", UserStatusType.Online);

            }).Start();
        }

        public static async void CommandLaunchAndHold(Message message)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                if (DoubleCheckAndHandleReset()) return;
                if (IsBusyWithOtherCommand()) return;

                statusheader = "!LaunchAndHold command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                UpdateStatusBar("!LaunchAndHold... Making sure the command is viable.", UserStatusType.Idle);

                TRANSFERINGCHECKPOINT = true;

                WorkingDiscordName = message.Author.Username;
                if (message.Author.GlobalName != null) WorkingDiscordName = message.Author.GlobalName;

                while (AFKCYCLE)
                {
                }

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Making sure I have the checkpoint and everything is correct...");

                //parse command text
                //!TransferCheckpoint [activity shorthand(!activities)] [(optional)master] [single word name given to the checkpoint. a-z, 1-9 only] BungieUsername#0000

                CommandLayout output = ParseStandardCommand(message.Content, "LaunchAndHold", true);
                if (!output.works)
                {
                    TRANSFERINGCHECKPOINT = false;
                    return;
                }
                WorkingUserName = output.guardianname;

                int charslot = GetCharSlotOfCheckpoint(output.activitykey, output.checkpointname);
                if (charslot == 0)
                {
                    TRANSFERINGCHECKPOINT = false;
                    return;
                }

                NavigateToActivityFromCharSelect(charslot, output.activity, output.master);

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Sending an invite to " + WorkingUserName + ". If I don't see someone join in the next 5 minutes I will return to idle mode.");
                InvitePlayer("/invite " + WorkingUserName);
                statussubtext = "Invite sent. Waiting for player to join.";
                UpdateTextDisplay();

                WaitForPlayerJoinOrbit(DateTime.Now.AddMinutes(5));

                UpdateStatusBar("!LaunchAndHold... Detected join, Launching momentarily.", UserStatusType.Idle);
                statussubtext = "Join detected. Launching activity.";
                UpdateTextDisplay();
                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Launching activity...");

                Task.Delay(2000).Wait();

                SpamLaunchButtonUntilWorks();

                WaitThruBlackscreens(output.activity);

                statussubtext = "Now boots on ground...";
                UpdateTextDisplay();

                flagBootsOnGround = true;
                HOLDINGLOAD = true;
                TRANSFERINGCHECKPOINT = false;

                UpdateStatusBar("!LaunchAndHold... Currently holding load for " + WorkingDiscordName + ". Run !endhold to stop.", UserStatusType.Idle);
                statusheader = "!LaunchAndHold command:";
                statussubtext = "Boots on ground. Going to AFK macro.";
                UpdateTextDisplay();

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Now boots on the ground and holding the load. Remember to run \"!endhold\" when you want me to stop.");

                Task.Delay(10000).Wait();

                //navigate to collections to afk
                VerifyControllerInput();
                NavigateToCollections();

                while (HOLDINGLOAD)
                {
                    if (!SingleAfkCycle()) break;

                    SendClick(new Point(50, 50));
                    InvitePlayer("/invite " + WorkingUserName);
                    Task.Delay(2000).Wait();

                    VerifyControllerInput();
                    Task.Delay(10000).Wait();
                }

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Endhold command processed. Returning to orbit...");

                Controller.SetButtonState(Xbox360Button.B, true);
                Task.Delay(200).Wait();
                Controller.SetButtonState(Xbox360Button.B, false);
                Task.Delay(500).Wait();
                Controller.SetButtonState(Xbox360Button.B, true);
                Task.Delay(200).Wait();
                Controller.SetButtonState(Xbox360Button.B, false);
                Task.Delay(500).Wait();
                Controller.SetButtonState(Xbox360Button.B, true);
                Task.Delay(200).Wait();
                Controller.SetButtonState(Xbox360Button.B, false);
                Task.Delay(500).Wait();

                ReturnToCharSelectFast();

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Idling...");

                UpdateStatusBar("Idle...", UserStatusType.Online);
            }).Start();
        }

        public static async void CommandWipe(Message message)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                //check if im currently grabbing a checkpoint.
                if (GRABBINGCHECKPOINT)
                {
                    statussubtext = "Queued wipe command...";
                    UpdateTextDisplay();

                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Making sure im boots on the ground, so that I can wipe.");
                    //check if im boots on the ground. idk how yet. maybe trying to swap menus with dpad and seeing what happens?
                    if (!flagBootsOnGround)
                    {
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId, "I'm not boots on the ground right now. Please try again in a minute.");
                        return;
                    }
                    //lmao explode.

                    statussubtext = "Wipe Command: Detonating :3";
                    UpdateTextDisplay();

                    Controller.SetAxisValue(Xbox360Axis.RightThumbY, STICK_BACK);
                    Controller.SetButtonState(Xbox360Button.Y, true);
                    Task.Delay(2000).Wait();
                    Controller.SetButtonState(Xbox360Button.Y, false);
                    Task.Delay(2000).Wait();
                    Controller.SetAxisValue(Xbox360Axis.RightThumbY, STICK_CENTER); ;
                    Controller.SetSliderValue(Xbox360Slider.RightTrigger, TRIGGER_PULLED);
                    Task.Delay(1000).Wait();
                    Controller.SetSliderValue(Xbox360Slider.RightTrigger, TRIGGER_RELEASED);
                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, ":boom::white_check_mark: :3");

                    statussubtext = "Wipe Command: I hope this worked.";
                    UpdateTextDisplay();
                }
                else
                {
                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "I'm not currently grabbing a checkpoint. That's so rude to ask me to do right now.");
                }

            }).Start();
        }

        public static async void RenameCheckpoint(Message message)
        {

            if (IsBusyWithOtherCommand()) return;

            string[] messagechunks = message.Content.Split(" ");
            //!RenameCheckpoint activity [master] checkpointname newcheckpointname
            if (messagechunks.Length < 4)
            {
                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Improper use of the \"!RenameCheckpoint\" command.\nTry \"!help RenameCheckpoint\" to learn how to use it.");
                return;
            }

            string oldname = messagechunks[2];
            string newname = messagechunks[3];

            string activity = messagechunks[1].ToUpper();
            string activitykey = activity;
            if (!CheckActivityExists("RenameCheckpoint", activity)) return;
            bool[] master = CheckMasterValue(messagechunks[2], activity);
            if (master[0])
            {
                if (!master[1]) return;
                oldname = messagechunks[3];
                newname = messagechunks[4];
                activitykey = "master" + activity;
            }

            int slot = GetCharSlotOfCheckpoint(activitykey, oldname);
            if (slot == 0) return;

            newname = CleanCheckpointName(newname);
            if (newname == "")
            {
                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Somehow you've managed to give me a name that when cleaned up returns an empty string\nCheckpoint names may only be letters and numbers.");
                return;
            }

            bool deleted = false;
            foreach (string key in Checkpoints[activitykey].Keys)
            {
                if (key.ToLower() == oldname.ToLower())
                {
                    Checkpoints[activitykey].Remove(key);
                    deleted = true;
                    break;
                }
            }

            if (!deleted)
            {
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I can't seem to delete that checkpoint to create a new instance of it. I'm not sure how this happened.");
                return;
            }

            Checkpoints[activitykey].Add(newname, slot);
            SaveCheckpoints();

            DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Successfully renamed " + oldname + " to " + newname + ".");

        }

        public static async void CommandEndFarm(Message message)
        {
            //check if a farm is even running.
            //ask for confirmation.
            //if no confirm in 60 seconds, forget this ever happened.
            //endfarm.
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                if (!FARMMODE)
                {
                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "I don't believe I'm currently running a farm. If I'm mistaken please run \"!ForceRestart\" to help me find where I am.");
                    return;
                }

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "I want to make sure we're on the same page. This will make me forget what I was doing and send me back to character select.\nSend \"!verify\" to confirm, or \"!cancel\" to cancel.\nIf no response is given in 60 seconds I will cancel on my own.");

                VERIFYING = true;
                VERIFYINGLEVEL = 0;

                DateTime timeout = DateTime.Now.AddMinutes(1);
                while (VERIFYINGLEVEL == 0)
                {
                    if (!VERIFYING) return;
                    if (DateTime.Now > timeout)
                    {
                        VERIFYING = false;
                        VERIFYINGLEVEL = 0;
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId, "No valid response given in time. Continuing without returning to orbit.");
                    }
                }

                VERIFYING = false;
                FARMMODE = false;

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Ending farm and returning to character select...");

                ReturnToCharSelectFast();

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Back to idle mode...");

                UpdateStatusBar("Idle...", UserStatusType.Online);

            }).Start();
        }

        public static async void CommandFarmCheckpoint(Message message)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                if (DoubleCheckAndHandleReset()) return;
                if (IsBusyWithOtherCommand()) return;

                statusheader = "!FarmCheckpoint command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                UpdateStatusBar("!FarmCheckpoint... Validating command.", UserStatusType.Idle);

                FARMMODE = true;

                WorkingDiscordName = message.Author.Username;
                if (message.Author.GlobalName != null) WorkingDiscordName = message.Author.GlobalName;

                while (AFKCYCLE)
                {
                }

                CommandLayout output = ParseStandardCommand(message.Content, "FarmCheckpoint", true);
                if (!output.works)
                {
                    FARMMODE = false;
                    return;
                }
                WorkingUserName = output.guardianname;

                int charslot = GetCharSlotOfCheckpoint(output.activitykey, output.checkpointname);
                if (charslot == 0)
                {
                    FARMMODE = false;
                    return;
                }

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Preparing farm :3");

                while (FARMMODE)
                {
                    if (!FARMMODE) //im already on character select if this triggers here
                    {
                        UpdateStatusBar("Idle...", UserStatusType.Online);
                        return;
                    }

                    NavigateToActivityFromCharSelect(charslot, output.activity, output.master);
                    if (!FARMMODE) break;

                    if (output.activitykey == "DP" || output.activitykey == "EDP" || output.activitykey == "EQ") ClearFeats();
                    if (output.feats) SelectFeats(output.featlist);
                    if (!FARMMODE) break;

                    InvitePlayer("/invite " + WorkingUserName);

                    statussubtext = "Invite sent. Waiting for player to join.";
                    UpdateTextDisplay();

                    UpdateStatusBar("!FarmCheckpoint... Invite sent to " + WorkingUserName + "... Awaiting their arrival.", UserStatusType.Idle);

                    Point pointcheck = ConvertAspectRatioCoords(95.859, 83.75);
                    Point pointclick = ConvertAspectRatioCoords(75.117, 83.75);

                    SetCursorPos(pointclick.X, pointclick.Y);

                    Task.Delay(1000).Wait();
                    if (!FARMMODE) break;

                    bool joined = WaitForPlayerJoinOrbit(DateTime.Now.AddMinutes(55));
                    if (!joined)
                    {
                        FARMMODE = false;
                        break;
                    }

                    UpdateStatusBar("!FarmCheckpoint... Join detected, launching momentarily.", UserStatusType.Idle);
                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Launching activity as soon as it lets me...");
                    statussubtext = "Join detected. Launching activity.";
                    UpdateTextDisplay();

                    Task.Delay(2000).Wait();

                    SpamLaunchButtonUntilWorks();

                    statussubtext = "Awaiting first black screen.";
                    UpdateTextDisplay();
                    DateTime bailout = DateTime.Now.AddSeconds(30);
                    while (!CheckBlackScreen())
                    {
                        if (DateTime.Now > bailout) break;
                        Task.Delay(30).Wait();
                    }

                    UpdateStatusBar("!FarmCheckpoint... Returning to orbit to prep again.", UserStatusType.Idle);
                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Returning to orbit to begin the cycle again.");
                    ReturnToCharSelectFast();
                }

                ReturnToCharSelectFast();
                UpdateStatusBar("Idle...", UserStatusType.Online);
            }).Start();
        }

        public static async void CommandListCheckpoint(Message message)
        {
            //make sure reset hasnt happened yet.
            if (DoubleCheckAndHandleReset()) return;

            //list out all checkpoints if no activity is specified. otherwise, list all checkpoints for the given activity. If there are none, explain that.
            string outputraid = "## Raids:\n";
            string outputdungeon = "## Dungeons:\n";
            string outputpantheon = "## Pantheon:\n";
            bool good = false;
            foreach (string key in Checkpoints.Keys)
            {
                if (Checkpoints[key].Count > 0)
                {
                    good = true;

                    string checkstr = "";
                    int i = 1;
                    foreach (string checkpointname in Checkpoints[key].Keys)
                    {
                        checkstr = checkstr + " - [" + Checkpoints[key][checkpointname] + "]" + checkpointname + "\n";
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

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, outputraid).Wait();
                DiscordClient.Rest.SendMessageAsync(message.ChannelId, outputdungeon).Wait();
                DiscordClient.Rest.SendMessageAsync(message.ChannelId, outputpantheon).Wait();
            }
            else
            {
                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "I currently have no checkpoints. :(");
            }
        }

        public static async void CommandDeleteCheckpoint(Message message)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                if (DoubleCheckAndHandleReset()) return;
                if (IsBusyWithOtherCommand()) return;

                statusheader = "!DeleteCheckpoint command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                UpdateStatusBar("!DeleteCheckpoint... Validating command.", UserStatusType.Idle);

                DELETINGCHECKPOINT = true;

                WorkingDiscordName = message.Author.Username;
                if (message.Author.GlobalName != null) WorkingDiscordName = message.Author.GlobalName;

                while (AFKCYCLE)
                {
                }

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Making sure I have the checkpoint and everything is correct...");

                //parse command text
                CommandLayout output = ParseStandardCommand(message.Content, "DeleteCheckpoint", false);
                if (!output.works)
                {
                    DELETINGCHECKPOINT = false;
                    return;
                }

                int charslot = GetCharSlotOfCheckpoint(output.activitykey, output.checkpointname);
                if (charslot == 0)
                {
                    DELETINGCHECKPOINT = false;
                    return;
                }

                UpdateStatusBar("!DeleteCheckpoint... Navigating to activity.", UserStatusType.Idle);

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Command acknowledged, navigating to the activity to delete the checkpoint.");

                NavigateToActivityFromCharSelect(charslot, output.activity, output.master);

                UpdateStatusBar("!DeleteCheckpoint... Removing checkpoint.", UserStatusType.Idle);
                RemoveCheckpoint();

                Checkpoints[output.activitykey].Remove(output.checkpointname);
                SaveCheckpoints();

                UpdateStatusBar("!DeleteCheckpoint... Returning to character select...", UserStatusType.Idle);
                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Checkpoint deleted, I'm returning to orbit now.");

                ReturnToCharSelectFast();
                DELETINGCHECKPOINT = false;

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Idling...");
                UpdateStatusBar("Idle...", UserStatusType.Online);
            }).Start();

            //check to see if the checkpoint exists. if it does, ask for confirmation. if not, make sure the user typed it correctly.
            //also make sure im not already waiting for confirmation somewhere.
            //if confirmed, delete checkpoint.
        }

        public static async void CommandEndHoldLoad(Message message)
        {
            //check to see if im even holdling a checkpoint. 
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                //verify.
                VERIFYING = true;
                VERIFYINGLEVEL = 0;

                string oldstatus = statusheader;
                string oldsubtext = statussubtext;

                statusheader = "Processing !EndHold command:";
                statussubtext = "Awaiting first verification.";
                UpdateTextDisplay();

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Are you sure you're done holding the load? If you're doing this to a load someone else is holding please make sure they're done.\nSend \"!verify\" to confirm or \"!cancel\" to cancel. If no message is sent in 60 seconds I'll go back to idleing.");

                DateTime timeout = DateTime.Now.AddMinutes(1);
                while (VERIFYINGLEVEL == 0)
                {
                    if (!VERIFYING) return;
                    if (DateTime.Now > timeout)
                    {
                        VERIFYING = false;
                        VERIFYINGLEVEL = 0;
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId, "No valid response given in time. Continuing without returning to orbit.");

                        statusheader = oldstatus;
                        statussubtext = oldsubtext;
                        UpdateTextDisplay();
                    }
                }
                VERIFYING = false;
                HOLDINGLOAD = false;
            }).Start();
        }

        public static async void CommandHoldLoad(Message message)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                if (IsBusyWithOtherCommand()) return;

                statusheader = "!HoldLoad command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                UpdateStatusBar("!HoldLoad... Validating command.", UserStatusType.Idle);

                HOLDINGLOAD = true;

                WorkingDiscordName = message.Author.Username;
                if (message.Author.GlobalName != null) WorkingDiscordName = message.Author.GlobalName;

                while (AFKCYCLE)
                {
                }

                //figure out what character to grab the checkpoint on. if my checkpoints are full, bail.
                string[] messagechunks = message.Content.Split(" ");
                //!GrabCheckpoint BungieUsername#0000
                if (messagechunks.Length < 2)
                {
                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Improper use of the \"!holdload\" command.\nTry \"!help holdload\" to learn how to use it.");
                    HOLDINGLOAD = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }

                string[] nameID = messagechunks.Last().Split("#");
                if (nameID.Length == 1 || nameID.Last().Split('#').Last().Length != 4)
                {
                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "For the \"!holdload\" command to work, I need the 4 number hashtag after your guardians name.\nTry \"!help holdload\" to learn how to use it.");
                    HOLDINGLOAD = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }

                WorkingUserName = "";
                for (int i = 1; i < messagechunks.Length; i++)
                {
                    if (WorkingUserName == "") WorkingUserName = WorkingUserName + messagechunks[i];
                    else WorkingUserName = WorkingUserName + " " + messagechunks[i];
                }

                int charslot = 1;

                //activity for the activity name (shorthand + master)
                //charslot for which character the checkpoint is stored on.
                //master (bool) to know if its master mode or not

                statusheader = "!HoldLoad command:";
                statussubtext = "Attempting to join...";
                UpdateTextDisplay();

                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Checking to see if I can join...");
                SelectChar(charslot);

                UpdateStatusBar("!HoldLoad... Joining fireteam.", UserStatusType.Idle);
                bool worked = JoinFireteamFromOrbit("/join " + WorkingUserName, "");
                if (!worked)
                {
                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Looks like your fireteam is currently unavailable. Returning to idling.");
                    HOLDINGLOAD = false;

                    if (CheckIfOrbitTextBox() != "")
                    {
                        //make sure im in controller mode
                        Controller.SetButtonState(Xbox360Button.RightThumb, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, false);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, false);
                        Task.Delay(101).Wait();

                        Controller.SetButtonState(Xbox360Button.B, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.B, false);
                        Task.Delay(101).Wait();
                    }

                    ReturnToCharSelectFast();

                    UpdateStatusBar("Idle...", UserStatusType.Online);

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();
                }
                else
                {

                    UpdateStatusBar("!HoldLoad... Currently holding load for " + WorkingDiscordName + ". Run !endhold to stop.", UserStatusType.Idle);
                    statusheader = "!HoldLoad command:";
                    statussubtext = "Boots on ground. Going to AFK macro.";
                    UpdateTextDisplay();

                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Now boots on the ground and holding the load. Remember to run \"!endhold\" when you want me to stop.");

                    Task.Delay(10000).Wait();

                    VerifyControllerInput();
                    NavigateToCollections();

                    while (HOLDINGLOAD)
                    {
                        if (!SingleAfkCycle()) break;

                        SendClick(new Point(50, 50));
                        InvitePlayer("/invite " + WorkingUserName);
                        Task.Delay(2000).Wait();

                        VerifyControllerInput();
                        Task.Delay(10000).Wait();
                    }

                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Endhold command processed. Returning to orbit...");

                    Controller.SetButtonState(Xbox360Button.B, true);
                    Task.Delay(200).Wait();
                    Controller.SetButtonState(Xbox360Button.B, false);
                    Task.Delay(500).Wait();
                    Controller.SetButtonState(Xbox360Button.B, true);
                    Task.Delay(200).Wait();
                    Controller.SetButtonState(Xbox360Button.B, false);
                    Task.Delay(500).Wait();
                    Controller.SetButtonState(Xbox360Button.B, true);
                    Task.Delay(200).Wait();
                    Controller.SetButtonState(Xbox360Button.B, false);
                    Task.Delay(500).Wait();

                    ReturnToCharSelectFast();

                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Idling...");

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                }
            }).Start();
        }

        public static async void CommandGrabCheckpoint(Message message)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                //make sure reset hasnt happened yet.

                if (DoubleCheckAndHandleReset()) return;
                if (IsBusyWithOtherCommand()) return;

                statusheader = "!GrabCheckpoint command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                UpdateStatusBar("!GrabCheckpoint... Validating command.", UserStatusType.Idle);

                GRABBINGCHECKPOINT = true;

                WorkingDiscordName = message.Author.Username;
                if (message.Author.GlobalName != null) WorkingDiscordName = message.Author.GlobalName;

                while (AFKCYCLE)
                {
                }

                CommandLayout output = ParseStandardCommand(message.Content, "GrabCheckpoint", true);
                if (!output.works)
                {
                    GRABBINGCHECKPOINT = false;
                    return;
                }

                GRABBINGCHECKPOINT = CheckOverlappingCheckpointName(output.checkpointname, output.activitykey, "FlyInCheckpointTransfer");
                if (!GRABBINGCHECKPOINT) return;
                GRABBINGCHECKPOINT = CheckCheckpointsFull(output.activitykey);
                if (!GRABBINGCHECKPOINT) return;

                int charslot = GetFirstEmptyCharSlot(output.activitykey);
                WorkingUserName = output.guardianname;

                statusheader = "!GrabCheckpoint command:";
                statussubtext = "Attempting to join...";
                UpdateTextDisplay();
                UpdateStatusBar("!GrabCheckpoint... Attempting to join " + WorkingUserName + ".", UserStatusType.Idle);
                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Attempting to join...");

                SelectChar(charslot);
                bool worked = JoinFireteamFromOrbit("/join " + WorkingUserName, output.activity);
                if (!worked)
                {
                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Looks like your fireteam is currently unavailable. Returning to idling.");
                    GRABBINGCHECKPOINT = false;

                    if (CheckIfOrbitTextBox() != "")
                    {
                        //make sure im in controller mode
                        Controller.SetButtonState(Xbox360Button.RightThumb, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, false);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, false);
                        Task.Delay(101).Wait();

                        Controller.SetButtonState(Xbox360Button.B, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.B, false);
                        Task.Delay(101).Wait();
                    }

                    ReturnToCharSelectFast();

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                }
                else
                {
                    //make sure its not an error code please TODO - not sure how to synthesize an error code here honestly.

                    //wait for wipe and then return to orbit

                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "I'm now pretty sure I'm boots on ground. Wipe when ready.");

                    statusheader = "!GrabCheckpoint command:";
                    statussubtext = "Boots on ground. Awaiting wipe screen.";
                    UpdateTextDisplay();
                    UpdateStatusBar("!GrabCheckpoint... Awaiting wipe :3", UserStatusType.Idle);

                    AwaitText("lightfadesaway", ConvertAspectRatioCoords(30.98958333333, 9.25925925925926), ConvertAspectRatioCoords(66.927083333, 15.55555555555));

                    statusheader = "!GrabCheckpoint command:";
                    statussubtext = "Wipe screen found. Waiting for wipe screen to clear.";
                    UpdateTextDisplay();

                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Wipe detected. As soon as the wipe screen closes I'll return to orbit.");

                    AwaitColorChange(50, 50, 2);

                    if (!output.master) DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Checkpoint " + output.checkpointname + " grabbed for " + output.activity + " from " + WorkingUserName + ".\n Returning to orbit to idle.");
                    else DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Checkpoint " + output.checkpointname + " grabbed for master " + output.activity + " from " + WorkingUserName + ".\n Returning to orbit to idle.");

                    Checkpoints[output.activitykey].Add(output.checkpointname, charslot);
                    SaveCheckpoints();

                    GRABBINGCHECKPOINT = false;
                    flagBootsOnGround = false;

                    UpdateStatusBar("!GrabCheckpoint... Returning to orbit.", UserStatusType.Idle);
                    ReturnToCharSelectFast();

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                }
            }).Start();
        }

        public static async void CommandGrabCheckpointAndConfirm(Message message)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                //make sure reset hasnt happened yet.

                if (DoubleCheckAndHandleReset()) return;
                if (IsBusyWithOtherCommand()) return;

                statusheader = "!GrabCheckpointAndConfirm command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                UpdateStatusBar("!GrabCheckpointAndConfirm... Validating command.", UserStatusType.Idle);

                GRABBINGCHECKPOINT = true;

                WorkingDiscordName = message.Author.Username;
                if (message.Author.GlobalName != null) WorkingDiscordName = message.Author.GlobalName;

                while (AFKCYCLE)
                {
                }

                CommandLayout output = ParseStandardCommand(message.Content, "GrabCheckpointAndConfirm", true);
                if (!output.works)
                {
                    GRABBINGCHECKPOINT = false;
                    return;
                }

                GRABBINGCHECKPOINT = CheckOverlappingCheckpointName(output.checkpointname, output.activitykey, "FlyInCheckpointTransfer");
                if (!GRABBINGCHECKPOINT) return;
                GRABBINGCHECKPOINT = CheckCheckpointsFull(output.activitykey);
                if (!GRABBINGCHECKPOINT) return;

                int charslot = GetFirstEmptyCharSlot(output.activitykey);
                WorkingUserName = output.guardianname;

                statusheader = "!GrabCheckpointAndConfirm command:";
                statussubtext = "Attempting to join...";
                UpdateTextDisplay();
                UpdateStatusBar("!GrabCheckpointAndConfirm... Attempting to join " + WorkingUserName + ".", UserStatusType.Idle);
                DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Attempting to join...");

                SelectChar(charslot);
                bool worked = JoinFireteamFromOrbit("/join " + WorkingUserName, output.activity);
                if (!worked)
                {
                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Looks like your fireteam is currently unavailable. Returning to idling.");
                    GRABBINGCHECKPOINT = false;

                    if (CheckIfOrbitTextBox() != "")
                    {
                        //make sure im in controller mode
                        Controller.SetButtonState(Xbox360Button.RightThumb, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, false);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, false);
                        Task.Delay(101).Wait();

                        Controller.SetButtonState(Xbox360Button.B, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.B, false);
                        Task.Delay(101).Wait();
                    }

                    ReturnToCharSelectFast();

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();
                    UpdateStatusBar("Idle...", UserStatusType.Online);
                }
                else
                {
                    //make sure its not an error code please TODO - not sure how to synthesize an error code here honestly.

                    //wait for wipe and then return to orbit

                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "I'm now pretty sure I'm boots on ground. Let me know when to leave with \"!endhold\".");

                    HOLDINGLOAD = true;

                    Task.Delay(10000).Wait();

                    VerifyControllerInput();
                    NavigateToCollections();

                    while (HOLDINGLOAD)
                    {
                        SingleAfkCycle();
                    }

                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "Endhold command processed. Returning to orbit...");

                    Controller.SetButtonState(Xbox360Button.B, true);
                    Task.Delay(200).Wait();
                    Controller.SetButtonState(Xbox360Button.B, false);
                    Task.Delay(500).Wait();
                    Controller.SetButtonState(Xbox360Button.B, true);
                    Task.Delay(200).Wait();
                    Controller.SetButtonState(Xbox360Button.B, false);
                    Task.Delay(500).Wait();
                    Controller.SetButtonState(Xbox360Button.B, true);
                    Task.Delay(200).Wait();
                    Controller.SetButtonState(Xbox360Button.B, false);

                    Task.Delay(500).Wait(); UpdateStatusBar("!GrabCheckpointAndConfirm: Making sure the checkpoint saved correctly.", UserStatusType.Idle);

                    statusheader = "!GrabCheckpointAndConfirm command:";
                    statussubtext = "Boots on ground. Returning to orbit.";
                    UpdateTextDisplay();

                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "I'm now double checking to make sure the checkpoint saved correctly.");

                    //return to char select, then see if the checkpoint saved.
                    ReturnToCharSelectFast();

                    VerifyCheckpointAndSave(charslot, output);

                    GRABBINGCHECKPOINT = false;
                    flagBootsOnGround = false;
                    ReturnToCharSelectFast();

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);

                    DiscordClient.Rest.SendMessageAsync(message.ChannelId, "GrabCheckpointAndConfirm complete. Back to idling...");
                }
            }).Start();
        }

        public static async void CommandActivities(Message message)
        {
            DiscordClient.Rest.SendMessageAsync(message.ChannelId, "## Raids:\n" +
                " - CE (Crota's End) \n" +
                " - DSC (Deep Stone Crypt) \n" +
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

        public static async void CommandListFeats(Message message)
        {
            //Token, Phase, Battalions, Challenges, and Cutthroat.
            DiscordClient.Rest.SendMessageAsync(message.ChannelId, "## Feats:\n" +
                " - Token (Token Limit) \n" +
                " - Phase (Phase Limit) \n" +
                " - Battalions \n" +
                " - Challenges (Encounter Challenges) \n" +
                " - Cutthroat (Cutthroat Combat)");
        }

        public static async void CommandListCommands(Message message)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            DiscordClient.Rest.SendMessageAsync(message.ChannelId,
                "## Available commands:\n" +
                " - **!Help:** !Help [command] to see a MUCH more detailed description of any of these commands, and how to use them.\n" +
                " - **!ListCommands:** I'll list all available commands and brief definitions.\n" +
                " - **!Activities:** I'll list all available activities.\n" +
                " - **!HoldLoad:** I'll hold a load by joining on the person until the !EndHold command is run. Good for grabbing bonus chests.\n" +
                " - **!EndHold:** Stops me holding a load.\n" +
                " - **!GrabCheckpoint:** Has me join on the username given with the command, and then wait for a wipe so that I have the checkpoint, at which point I'll then return to orbit and idle.\n" +
                " - **!ForceWipe:** If applicable, I'll fire a rocket at the ground to force a wipe.\n" +
                " - **!LaunchAndHold:** I'll launch a checkpoint and afk on the ground, occasionally attempting to send an invite to the author so that people may grab bonus chests from a checkpoint.\n" +
                " - **!DeleteCheckpoint:** I'll delete a checkpoint so that it may be replaced with another.\n").Wait();
            DiscordClient.Rest.SendMessageAsync(message.ChannelId,
                " - **!ListCheckpoints:** Used to list what checkpoints I have. \n" +
                " - **!ListFeats:** Used to list what feats are available. \n" +
                " - **!FarmCheckpoint:** Used to target farm a specific encounter.\n" +
                " - **!EndFarm:** I'll stop farming the given activity and shift into idle mode.\n" +
                " - **!TransferCheckpoint:** Used to transfer a checkpoint from me to you.\n" +
                " - **!FlyInCheckpointTransfer:** Allows giving checkpoints to me without the use of a darkness zone or wipe. warning: complicated.\n" +
                " - **!GrabCheckpointAndConfirm:** I'll join on you and then afk until you tell me I should have a checkpoint. Then I'll check if I've aquired a new checkpoint once I bail and echo the results back.\n" +
                " - **!RenameCheckpoint:** Renames a checkpoint.\n" +
                " - **!CleanCheckpoints:** Takes half an hour, I'll go thru and clean out any vestigial checkpoints I have no memory of.\n" +
                " - **!ForceRestart:** Shuts down the entire bot and restarts it. Useful if I'm bugging out.\n");

#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        public static async void CommandHelp(Message message)
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
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId,
                            "### !ListCommands:\n" +
                            " - I'll list all available commands and shorthand definitions of them.\n" +
                            " - Usage: !ListCommands\n");
                        done = true;
                        return;
                    case "launchandhold":
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId,
                            "### !LaunchAndHold \n" +
                            " - Has me load into a checkpoint and stay boots on the ground until the \"!endhold\" command is run. I will occasionally send invites to the person who ran the command so they may rejoin.\n" +
                            " - Usage: !LaunchAndHold [activity shorthand(!activities)] [(optional)master] [single word name given to the checkpoint.  a-z, 0-9 only] BungieUsername#0000");
                        done = true;
                        return;
                    case "activities":
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId,
                        "### !Activities\n" +
                        " - I'll list all available activities.\n" +
                        " - usage: !Activities\n");
                        done = true;
                        return;
                    case "holdload":
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId,
                        "### !HoldLoad: \n" +
                        " - I'll hold a load by joining on the person until the !EndHold command is run.\n" +
                        " - usage: !HoldLoad BungieUsername#0000\n");
                        done = true;
                        return;
                    case "endhold":
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId,
                            "### !EndHold: \n" +
                            " - First I will confirm that you do want to end the hold, with !verify.\n" +
                            " - After that, I'll return to orbit and enter idle mode.\n" +
                            " - usage: !EndHold \n");
                        done = true;
                        return;
                    case "grabcheckpoint":
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId,
                            "### !GrabCheckpoint: \n" +
                            " - Has me join on the username given with the command, and then wait for a wipe so that I have the checkpoint, at which point I'll then return to orbit and idle.\n" +
                            " - usage: !GrabCheckpoint [activity shorthand (!activities)] [(optional)master] [single word name for the checkpoint of your choosing.  a-z, 1-9 only] BungieUsername#0000 \n" +
                            " - example: !GrabCheckpoint WR ogre ItAvvy#7006\n" +
                            " - If I already have a checkpoint in that activity on all 3 characters I will list them and ask you to delete one using !deletecheckpoint.");
                        done = true;
                        return;
                    case "grabcheckpointandconfirm":
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId,
                            "### !GrabCheckpointAndConfirm: \n" +
                            " - Has me join on the username given with the command, and then wait for confirmation that I should leave, at which point I'll then return to orbit and confirm that I got the checkpoint.\n" +
                            " - When you're ready for me to go confirm that I have the checkpoint run the \"!endhold\" command.\n" +
                            " - usage: !GrabCheckpointAndConfirm [activity shorthand (!activities)] [(optional)master] [single word name for the checkpoint of your choosing.  a-z, 1-9 only] BungieUsername#0000 \n" +
                            " - example: !GrabCheckpointAndConfirm WR ogre ItAvvy#7006\n" +
                            " - If I already have a checkpoint in that activity on all 3 characters I will list them and ask you to delete one using !deletecheckpoint.");
                        done = true;
                        return;
                    case "deletecheckpoint":
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId,
                            "### !DeleteCheckpoint:\n" +
                            " - I'll delete a checkpoint from a given activity with a given name so that a new checkpoint may be gotten on that character.\n" +
                            " - usage: !DeleteCheckpoint [activity shorthand (!activities)] [(optional)master] [single word name of the checkpoint. a-z, 0-9 only] \n" +
                            " - example: !DeleteCheckpoint WR master ogre");
                        done = true;
                        return;
                    case "listcheckpoints":
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId,
                            "### !ListCheckpoints: \n" +
                            " - Lists out all checkpoints, and specifies master in cases where its applicable.\n" +
                            " - usage: !ListCheckpoints");
                        done = true;
                        return;
                    case "listfeats":
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId,
                            "### !ListFeats: \n" +
                            " - Lists out all feats as I recognise them.\n" +
                            " - usage: !ListFeats");
                        done = true;
                        return;
                    case "farmcheckpoint":
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId,
                            "### !FarmCheckpoint: \n" +
                            " - I'll load the character the given checkpoint is on, and I'll wait in orbit for you to join. The moment you join I'll launch the activity, transferring the checkpoint on load-in. Then, I will return to orbit to wait to launch again. Use !EndFarm to end the farm. If you specify feats with the optional modifier, I will try to launch the checkpoint with those feats if applicable.\n" +
                            " - Viable Feats: Token, Phase, Battalions, Challenges, and Cutthroat. \n" +
                            " - usage: !FarmCheckpoint [activity shorthand (!activities)] [(optional)master] [(optional)feats:feat1name,feat2name,etc...] [single word name for the checkpoint of your choosing. a-z, 0-9 only]  BungieUsername#0000 \n" +
                            " - example: !FarmCheckpoint EQ feats:tokenlimit,phaselimit shockyhands ItAvvy#7006");
                        done = true;
                        return;
                    case "endfarm":
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId,
                            "### !EndFarm: \n" +
                            " - I'll stop farming the given activity and shift into idle mode. I will then ask for you to run !verify to verify that you do in fact want to end the farm.\n" +
                            " - Usage: !EndFarm");
                        done = true;
                        return;
                    case "forcewipe":
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId,
                            "### !ForceWipe: \n" +
                            " - If applicable, I'll fire a rocket at the ground to force a wipe.\n" +
                            " - usage: !ForceWipe");
                        done = true;
                        return;
                    case "help":
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId,
                            "### !Help: \n" +
                            " - Why are you asking for help with the \"help\" command???\n" +
                            " - usage: !Help [command name]");
                        done = true;
                        return;
                    case "transfercheckpoint":
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId,
                            "### !TransferCheckpoint \n" +
                            " - Has me load into a checkpoint only a single time after someone joins my lobby, so that I may transfer the checkpoint to them, and then I'll return to idle.\n" +
                            " - Usage: !TransferCheckpoint [activity shorthand(!activities)] [(optional)master] [single word name given to the checkpoint.  a-z, 0-9 only] BungieUsername#0000");
                        done = true;
                        return;
                    case "flyincheckpointtransfer":
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId,
                            "### !FlyInCheckpointTransfer\n" +
                            " - Transfers a checkpoint from you, to me, the hard way without using a darkness zone.\n" +
                            " - Warning, this requires a bit of cooperation, and will only work if your fireteam is set to open.\n" +
                            " - First, navigate on your director to the activity that has the checkpoint you want to transfer on it, and wait in orbit. Then run this command.\n" +
                            " - After you run the command I will attempt to join on you.\n" +
                            " - Then, I'll ask you to launch the activity, open your inventory, navigate to \"change character\" in your settings, click it, and then have you wait to confirm.\n" +
                            " - Once my screen goes black, I'll send a chat message telling you to hit confirm to change characters.\n" +
                            " - Once I'm boots on the ground I will return to orbit, verify that I do have the checkpoint, and echo the result here.\n" +
                            " - Usage: !flyincheckpointtransfer [activity shorthand (!activities)] [(optional)master] [single word name of the checkpoint.  a-z, 1-9 only] BungieUsername#0000");
                        done = true;
                        return;
                    case "cleancheckpoints":
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId,
                            "### !CleanCheckpoints\n" +
                            " - I will go thru, activity by activity, both normal and master and delete any erronious checkpoints I may have that I don't have record of.\n" +
                            " - This does require you to verify that you want to do it beforehand, as it takes about 30 minutes to go thru everything.\n" +
                            " - Usage: !CleanCheckpoints");
                        done = true;
                        return;
                    case "forcerestart":
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId,
                            "### !ForceRestart\n" +
                            " - I will first verify you want to do this. Twice.\n" +
                            " - After verifying, I will kill the d2 process, and then restart myself to have a \"blank slate\" so to speak, and to unstuck myself.\n" +
                            " - This __DOES NOT__ delete any checkpoints.\n" +
                            " - Usage: !ForceRestart");
                        done = true;
                        return;
                    case "renamecheckpoint":
                        DiscordClient.Rest.SendMessageAsync(message.ChannelId,
                            "### !RenameCheckpoint \n" +
                            " - I'll rename a checkpoint for you to something else.\n" +
                            " - Usage: !RenameCheckpoint [activity shorthand(!activities)] [(optional)master] [single word name given to the checkpoint] [new name for given checkpoint. a-z, 0-9 only]");
                        done = true;
                        return;
                }
                if (!done)
                {
                    DiscordClient.Rest.SendMessageAsync(message.ChannelId,
                        "The command you asked about doesn't seem to exist.\nPlease verify your spelling and try again or use the !ListCommands command.");
                }
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

        public static async void CommandGerbCheckpoint(Message message)
        {
            DiscordClient.Rest.SendMessageAsync(message.ChannelId, "gerbulating...");
            Thread.Sleep(3000);
            DiscordClient.Rest.SendMessageAsync(message.ChannelId, "gerbulation failed :(");
        } //this command is a bit for my discord. You're welcome to ignore or delete it.
    }

    public class SlashCommand : ApplicationCommandModule<SlashCommandContext>
    {
        [SlashCommand("transfercheckpoint", "I'll transfer a checkpoint to you via fly in transfer.")]
        public void SlashCommandTransferCheckpoint( //!transfercheckpoint [activity shorthand (!activities)] [(optional)master] [single word name for the checkpoint of your choosing.  a-z, 1-9 only] BungieUsername#0000
            [SlashCommandParameter(Name = "activity", Description = "the activity to launch", AutocompleteProviderType = typeof(ActivitiesWithCheckpoints))] string @activity,
            [SlashCommandParameter(Name = "master", Description = "enable master difficulty", AutocompleteProviderType = typeof(IsMasterAutoComplete))] string @master,
            [SlashCommandParameter(Name = "checkpoint", Description = "name of the checkpoint", AutocompleteProviderType = typeof(CheckpointAutocomplete))] string @checkpoint,
            [SlashCommandParameter(Name = "username", Description = "your bungie username including #0000")] string @bungieID
        )
        {
            if(Context.Channel.Id != DiscordChannelID)
            {
                Context.Interaction.SendResponseAsync(InteractionCallback.Message("I have a designated channel I'm intended to work in. Please use that channel."));
                return;
            }
            Context.Interaction.SendResponseAsync(InteractionCallback.Message("Transfer Checkpoint:"));
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                if (VERIFYING)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm currently waiting to verify a previous command. please be patient.");
                    return;
                }
                if (activity == null || master == null || checkpoint == null || bungieID == null)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Null value recieved, cancelling command.\nMake sure in the future you use all the available perameters.");
                    return;
                }
                if (activity == "" || master == "" || checkpoint == "" || bungieID == "")
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Null value recieved, cancelling command.\nMake sure in the future you use all the available perameters.");
                    return;
                }

                //make sure reset hasnt happened yet.

                if (DoubleCheckAndHandleReset()) return;
                if (IsBusyWithOtherCommand()) return;

                statusheader = "!TransferCheckpoint command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                UpdateStatusBar("!TransferCheckpoint... Making sure the command is viable.", UserStatusType.Idle);

                TRANSFERINGCHECKPOINT = true;

                User user = Context.User;

                WorkingDiscordName = user.Username;
                if (user.GlobalName != null) WorkingDiscordName = user.GlobalName;

                while (AFKCYCLE)
                {
                }

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Making sure I have the checkpoint and everything is correct...");

                //parse command text
                //!TransferCheckpoint [activity shorthand(!activities)] [(optional)master] [single word name given to the checkpoint. a-z, 1-9 only] BungieUsername#0000
                string standardizedcommand = "deadspace " + activity + " ";
                if (master.ToLower() == "true") standardizedcommand = standardizedcommand + "master ";
                standardizedcommand = standardizedcommand + checkpoint + " " + bungieID;

                CommandLayout output = ParseStandardCommand(standardizedcommand, "TransferCheckpoint", true);
                if (!output.works)
                {
                    TRANSFERINGCHECKPOINT = false;
                    return;
                }

                int charslot = GetCharSlotOfCheckpoint(output.activitykey, output.checkpointname);
                if (charslot == 0)
                {
                    TRANSFERINGCHECKPOINT = false;
                    return;
                }

                NavigateToActivityFromCharSelect(charslot, output.activity, output.master);

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Sending an invite to " + WorkingUserName + ". If I don't see someone join in the next 5 minutes I will return to idle mode.");
                InvitePlayer("/invite " + WorkingUserName);

                statussubtext = "Invite sent. Waiting for player to join.";
                UpdateTextDisplay();
                UpdateStatusBar("!TransferCheckpoint... Waiting for " + WorkingUserName + " to join.", UserStatusType.Idle);

                bool worked = WaitForPlayerJoinOrbit(DateTime.Now.AddMinutes(5));
                if (!worked)
                {
                    TRANSFERINGCHECKPOINT = false;
                    ReturnToCharSelectFast();
                    return;
                }

                UpdateStatusBar("!TransferCheckpoint... Detected join, Launching momentarily.", UserStatusType.Idle);
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Launching activity, and then returning to orbit. Good luck on your run(s).");

                Task.Delay(2000).Wait();

                SpamLaunchButtonUntilWorks();

                Task.Delay(3000).Wait();

                DateTime bailout = DateTime.Now.AddSeconds(30);
                while (!CheckBlackScreen())
                {
                    if (DateTime.Now > bailout) break;
                    Task.Delay(30).Wait();
                }

                TRANSFERINGCHECKPOINT = false;

                ReturnToCharSelectFast();

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Idling...");

                UpdateStatusBar("Idle...", UserStatusType.Online);

            }).Start();
            Context.Interaction.SendResponseAsync(InteractionCallback.Pong);
        }


        [SlashCommand("forcerestart", "Force the bot to restart after verifying.")]
        public void SlashCommandForceRestart()
        {
            if (Context.Channel.Id != DiscordChannelID)
            {
                Context.Interaction.SendResponseAsync(InteractionCallback.Message("I have a designated channel I'm intended to work in. Please use that channel."));
                return;
            }
            Context.Interaction.SendResponseAsync(InteractionCallback.Message("Force Restart:"));
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                if (VERIFYING)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Stopping previous verification. Gimme 5 seconds...");
                    VERIFYING = false;
                    Task.Delay(5000).Wait();
                }

                WorkingUserName = Context.User.Username;
                if (Context.User.GlobalName != null) WorkingUserName = Context.User.GlobalName;

                //verify.
                VERIFYING = true;
                VERIFYINGLEVEL = 0;

                string oldstatus = statusheader;
                string oldsubtext = statussubtext;

                statusheader = "Processing !ForceRestart command:";
                statussubtext = "Awaiting first verification.";
                UpdateTextDisplay();

                UpdateStatusBar("!ForceRestart: Verification step 1/2", UserStatusType.Idle);

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I want to make sure we're on the same page. This will make me forget what I was doing and send me back to character select.\nSend \"!verify\" to confirm, or \"!cancel\" to cancel.\nIf no response is given in 60 seconds I will cancel on my own.");

                DateTime timeout = DateTime.Now.AddMinutes(1);
                while (VERIFYINGLEVEL == 0)
                {
                    if (!VERIFYING) return;
                    if (DateTime.Now > timeout)
                    {
                        VERIFYING = false;
                        VERIFYINGLEVEL = 0;
                        DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "No valid response given in time. Continuing without returning to orbit.");

                        statusheader = oldstatus;
                        statussubtext = oldsubtext;
                        UpdateTextDisplay();
                        UpdateStatusBar("Idle...", UserStatusType.Online);
                        return;
                    }
                }
                //verify again.

                statusheader = "Processing !ForceRestart command:";
                statussubtext = "Awaiting second verification.";
                UpdateTextDisplay();

                UpdateStatusBar("!ForceRestart: Verification step 2/2", UserStatusType.Idle);

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm double checking. \"!verify\" to verify again, \"!cancel\" to cancel.");
                timeout = DateTime.Now.AddMinutes(1);
                while (VERIFYINGLEVEL == 1)
                {
                    if (!VERIFYING) return;
                    if (DateTime.Now > timeout)
                    {
                        VERIFYING = false;
                        VERIFYINGLEVEL = 0;
                        DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "No valid response given in time. Continuing without returning to orbit.");

                        statusheader = oldstatus;
                        statussubtext = oldsubtext;
                        UpdateTextDisplay();
                        UpdateStatusBar("Idle...", UserStatusType.Online);
                        return;
                    }
                }
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Restarting everything. o7").Wait();

                VERIFYING = false;
                VERIFYINGLEVEL = 0;

                D2Process.Kill();

            }).Start();
        }


        [SlashCommand("cleancheckpoints", "I'll spend about 30 minutes cleaning vestigial checkpoints.")]
        public void SlashCommandCleanCheckpoints()
        {
            if (Context.Channel.Id != DiscordChannelID)
            {
                Context.Interaction.SendResponseAsync(InteractionCallback.Message("I have a designated channel I'm intended to work in. Please use that channel."));
                return;
            }
            Context.Interaction.SendResponseAsync(InteractionCallback.Message("Clean Checkpoints:"));
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                if (VERIFYING)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm currently waiting to verify a previous command. please be patient.");
                    return;
                }

                if (DoubleCheckAndHandleReset()) return;
                if (IsBusyWithOtherCommand()) return;

                statusheader = "!CleanCheckpoints command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                VERIFYING = true;
                VERIFYINGLEVEL = 0;

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "This command takes about 30 minutes to run. I want to make sure we're on the same page here. run \"!verify\" to proceed.");

                DateTime timeout = DateTime.Now.AddMinutes(1);
                while (VERIFYINGLEVEL == 0)
                {
                    if (!VERIFYING) return;
                    if (DateTime.Now > timeout)
                    {
                        VERIFYING = false;
                        VERIFYINGLEVEL = 0;
                        DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "No valid response given in time. Continuing without returning to orbit.");
                    }
                }
                VERIFYING = false;
                CLEANINGCHECKPOINTS = true;

                WorkingDiscordName = Context.User.Username;
                if (Context.User.GlobalName != null) WorkingDiscordName = Context.User.GlobalName;

                while (AFKCYCLE)
                {
                }

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm now cleaning checkpoints. Estimated finish time is 30 minutes after this message.");

                CleanCheckpoints();

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Cleaning checkpoints is done, I'm back to idling...");

                CLEANINGCHECKPOINTS = false;

                UpdateStatusBar("Idle...", UserStatusType.Online);

            }).Start();
        }


        [SlashCommand("flyincheckpointtransfer", "I'll grab a checkpoint from you via fly in transfer.")]
        public void SlashCommandFlyInCheckpointTransfer( //!transfercheckpoint [activity shorthand (!activities)] [(optional)master] [single word name for the checkpoint of your choosing.  a-z, 1-9 only] BungieUsername#0000
            [SlashCommandParameter(Name = "activity", Description = "the activity to launch", ChoicesProviderType = typeof(ActivityProvider))] string @activity,
            [SlashCommandParameter(Name = "master", Description = "enable master difficulty", AutocompleteProviderType = typeof(CanMaster))] string @master,
            [SlashCommandParameter(Name = "checkpoint", Description = "single word name of the checkpoint")] string @checkpoint,
            [SlashCommandParameter(Name = "username", Description = "your bungie username including #0000")] string @bungieID
        )
        {
            if (Context.Channel.Id != DiscordChannelID)
            {
                Context.Interaction.SendResponseAsync(InteractionCallback.Message("I have a designated channel I'm intended to work in. Please use that channel."));
                return;
            }
            Context.Interaction.SendResponseAsync(InteractionCallback.Message("Fly-In Checkpoint Transfer:"));
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                if (VERIFYING)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm currently waiting to verify a previous command. please be patient.");
                    return;
                }
                if (activity == null || master == null || checkpoint == null || bungieID == null)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Null value recieved, cancelling command.\nMake sure in the future you use all the available perameters.");
                    return;
                }
                if (activity == "" || master == "" || checkpoint == "" || bungieID == "")
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Null value recieved, cancelling command.\nMake sure in the future you use all the available perameters.");
                    return;
                }

                //make sure reset hasnt happened yet.
                if (DoubleCheckAndHandleReset()) return;
                if (IsBusyWithOtherCommand()) return;

                statusheader = "!FlyInCheckpointTransfer command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                UpdateStatusBar("!FlyInCheckpointTransfer... Making sure the command is viable.", UserStatusType.Idle);

                GRABBINGCHECKPOINT = true;

                WorkingDiscordName = Context.User.Username;
                if (Context.User.GlobalName != null) WorkingDiscordName = Context.User.GlobalName;

                while (AFKCYCLE)
                {
                    //stall till not afk
                }

                //parse command text
                //!TransferCheckpoint [activity shorthand(!activities)] [(optional)master] [single word name given to the checkpoint. a-z, 1-9 only] BungieUsername#0000
                string standardizedcommand = "deadspace " + activity + " ";
                if (master.ToLower() == "true") standardizedcommand = standardizedcommand + "master ";
                standardizedcommand = standardizedcommand + checkpoint + " " + bungieID;

                CommandLayout output = ParseStandardCommand(standardizedcommand, "FlyInCheckpointTransfer", true); //this variable holds everything about the activity
                if (!output.works)
                {
                    GRABBINGCHECKPOINT = false;
                    return;
                }

                GRABBINGCHECKPOINT = CheckOverlappingCheckpointName(output.checkpointname, output.activitykey, "FlyInCheckpointTransfer");
                if (!GRABBINGCHECKPOINT) return;
                GRABBINGCHECKPOINT = CheckCheckpointsFull(output.activitykey);
                if (!GRABBINGCHECKPOINT) return;

                int charslot = GetFirstEmptyCharSlot(output.activitykey);
                WorkingUserName = output.guardianname;


                //BEGIN MACRO
                statusheader = "!FlyInCheckpointTransfer command:";
                statussubtext = "Attempting to join...";
                UpdateTextDisplay();

                UpdateStatusBar("!FlyInCheckpointTransfer: Attempting to join...", UserStatusType.Idle);

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Checking to see if I can join...");
                SelectChar(charslot);

                bool worked = JoinFireteamInOrbit("/join " + WorkingUserName);
                if (!worked)
                {
                    statussubtext = "Error code detected :(";
                    UpdateTextDisplay();

                    if (CheckIfOrbitTextBox() != "") //ERROR CODE
                    {
                        //make sure im in controller mode
                        Controller.SetButtonState(Xbox360Button.RightThumb, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, false);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, false);
                        Task.Delay(101).Wait();

                        Controller.SetButtonState(Xbox360Button.B, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.B, false);
                        Task.Delay(101).Wait();
                    }

                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Looks like your fireteam is currently unavailable. Returning to idling.");
                    GRABBINGCHECKPOINT = false;

                    ReturnToCharSelectFast();

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                }
                else
                {
                    InputSimulator sim = new InputSimulator();

                    //joining
                    statussubtext = "Join successful. Detecting first black screen...";
                    UpdateTextDisplay();

                    sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                    Task.Delay(1000).Wait();
                    sim.Keyboard.TextEntry("You may now launch the map, and get prepared to change characters.");
                    Task.Delay(1000).Wait();

                    sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                    Task.Delay(3000).Wait();

                    sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                    while (!CheckBlackScreen()) //wait until on next black screen
                    {
                        Task.Delay(20).Wait();
                    } //found black screen

                    //send notice message
                    sim.Keyboard.TextEntry("Please change characters now.");
                    Task.Delay(101).Wait();
                    sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                    while (CheckBlackScreen()) //wait for black screen to end, then return to char select
                    {
                        Task.Delay(20).Wait();
                    }

                    UpdateStatusBar("!FlyInCheckpointTransfer: Making sure the checkpoint saved correctly.", UserStatusType.Idle);

                    statusheader = "!FlyInCheckpointTransfer command:";
                    statussubtext = "Boots on ground. Returning to orbit.";
                    UpdateTextDisplay();

                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm now double checking to make sure the checkpoint saved correctly.");

                    //return to char select, then see if the checkpoint saved.
                    ReturnToCharSelectFast();

                    VerifyCheckpointAndSave(charslot, output);

                    ReturnToCharSelectFast();

                    GRABBINGCHECKPOINT = false;
                    flagBootsOnGround = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);

                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "FlyInCheckpointTransfer complete. Back to idling...");
                }
            }).Start();
        }


        [SlashCommand("launchandhold", "I'll launch a checkpoint and then hold the load.")]
        public void SlashCommandLaunchAndHold( //!transfercheckpoint [activity shorthand (!activities)] [(optional)master] [single word name for the checkpoint of your choosing.  a-z, 1-9 only] BungieUsername#0000
            [SlashCommandParameter(Name = "activity", Description = "the activity to launch", AutocompleteProviderType = typeof(ActivitiesWithCheckpoints))] string @activity,
            [SlashCommandParameter(Name = "master", Description = "enable master difficulty", AutocompleteProviderType = typeof(IsMasterAutoComplete))] string @master,
            [SlashCommandParameter(Name = "checkpoint", Description = "name of the checkpoint", AutocompleteProviderType = typeof(CheckpointAutocomplete))] string @checkpoint,
            [SlashCommandParameter(Name = "username", Description = "your bungie username including #0000")] string @bungieID
        )
        {
            if (Context.Channel.Id != DiscordChannelID)
            {
                Context.Interaction.SendResponseAsync(InteractionCallback.Message("I have a designated channel I'm intended to work in. Please use that channel."));
                return;
            }
            Context.Interaction.SendResponseAsync(InteractionCallback.Message("Launch and Hold:"));
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                if (VERIFYING)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm currently waiting to verify a previous command. please be patient.");
                    return;
                }
                if (activity == null || master == null || checkpoint == null || bungieID == null)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Null value recieved, cancelling command.\nMake sure in the future you use all the available perameters.");
                    return;
                }
                if (activity == "" || master == "" || checkpoint == "" || bungieID == "")
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Null value recieved, cancelling command.\nMake sure in the future you use all the available perameters.");
                    return;
                }

                if (DoubleCheckAndHandleReset()) return;
                if (IsBusyWithOtherCommand()) return;

                statusheader = "!LaunchAndHold command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                UpdateStatusBar("!LaunchAndHold... Making sure the command is viable.", UserStatusType.Idle);

                TRANSFERINGCHECKPOINT = true;

                WorkingDiscordName = Context.User.Username;
                if (Context.User.GlobalName != null) WorkingDiscordName = Context.User.GlobalName;

                while (AFKCYCLE)
                {
                }

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Making sure I have the checkpoint and everything is correct...");

                //parse command text
                //!TransferCheckpoint [activity shorthand(!activities)] [(optional)master] [single word name given to the checkpoint. a-z, 1-9 only] BungieUsername#0000
                string standardizedcommand = "deadspace " + activity + " ";
                if (master.ToLower() == "true") standardizedcommand = standardizedcommand + "master ";
                standardizedcommand = standardizedcommand + checkpoint + " " + bungieID;

                CommandLayout output = ParseStandardCommand(standardizedcommand, "LaunchAndHold", true);
                if (!output.works)
                {
                    TRANSFERINGCHECKPOINT = false;
                    return;
                }
                WorkingUserName = output.guardianname;

                int charslot = GetCharSlotOfCheckpoint(output.activitykey, output.checkpointname);
                if (charslot == 0)
                {
                    TRANSFERINGCHECKPOINT = false;
                    return;
                }

                NavigateToActivityFromCharSelect(charslot, output.activity, output.master);

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Sending an invite to " + WorkingUserName + ". If I don't see someone join in the next 5 minutes I will return to idle mode.");
                InvitePlayer("/invite " + WorkingUserName);
                statussubtext = "Invite sent. Waiting for player to join.";
                UpdateTextDisplay();

                WaitForPlayerJoinOrbit(DateTime.Now.AddMinutes(5));

                UpdateStatusBar("!LaunchAndHold... Detected join, Launching momentarily.", UserStatusType.Idle);
                statussubtext = "Join detected. Launching activity.";
                UpdateTextDisplay();
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Launching activity...");

                Task.Delay(2000).Wait();

                SpamLaunchButtonUntilWorks();

                WaitThruBlackscreens(output.activity);

                statussubtext = "Now boots on ground...";
                UpdateTextDisplay();

                flagBootsOnGround = true;
                HOLDINGLOAD = true;
                TRANSFERINGCHECKPOINT = false;

                UpdateStatusBar("!LaunchAndHold... Currently holding load for " + WorkingDiscordName + ". Run !endhold to stop.", UserStatusType.Idle);
                statusheader = "!LaunchAndHold command:";
                statussubtext = "Boots on ground. Going to AFK macro.";
                UpdateTextDisplay();

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Now boots on the ground and holding the load. Remember to run \"!endhold\" when you want me to stop.");

                Task.Delay(10000).Wait();

                //navigate to collections to afk
                VerifyControllerInput();
                NavigateToCollections();

                while (HOLDINGLOAD)
                {
                    if (!SingleAfkCycle()) break;

                    SendClick(new Point(50, 50));
                    InvitePlayer("/invite " + WorkingUserName);
                    Task.Delay(2000).Wait();

                    VerifyControllerInput();
                    Task.Delay(10000).Wait();
                }

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Endhold command processed. Returning to orbit...");

                Controller.SetButtonState(Xbox360Button.B, true);
                Task.Delay(200).Wait();
                Controller.SetButtonState(Xbox360Button.B, false);
                Task.Delay(500).Wait();
                Controller.SetButtonState(Xbox360Button.B, true);
                Task.Delay(200).Wait();
                Controller.SetButtonState(Xbox360Button.B, false);
                Task.Delay(500).Wait();
                Controller.SetButtonState(Xbox360Button.B, true);
                Task.Delay(200).Wait();
                Controller.SetButtonState(Xbox360Button.B, false);
                Task.Delay(500).Wait();

                ReturnToCharSelectFast();

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Idling...");

                UpdateStatusBar("Idle...", UserStatusType.Online);
            }).Start();
        }


        [SlashCommand("forcewipe", "If I'm grabbing a checkpoint I'll try to wipe.")]
        public void SlashCommandForceWipe()
        {
            if (Context.Channel.Id != DiscordChannelID)
            {
                Context.Interaction.SendResponseAsync(InteractionCallback.Message("I have a designated channel I'm intended to work in. Please use that channel."));
                return;
            }
            Context.Interaction.SendResponseAsync(InteractionCallback.Message(":boom:"));
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                if (VERIFYING)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm currently waiting to verify a previous command. please be patient.");
                    return;
                }
                //check if im currently grabbing a checkpoint.
                if (GRABBINGCHECKPOINT)
                {
                    statussubtext = "Queued wipe command...";
                    UpdateTextDisplay();

                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Making sure im boots on the ground, so that I can wipe.");
                    if (!flagBootsOnGround)
                    {
                        DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm not boots on the ground right now. Please try again in a minute.");
                        return;
                    }
                    //lmao explode.

                    statussubtext = "Wipe Command: Detonating :3";
                    UpdateTextDisplay();

                    Controller.SetAxisValue(Xbox360Axis.RightThumbY, STICK_BACK);
                    Controller.SetButtonState(Xbox360Button.Y, true);
                    Task.Delay(2000).Wait();
                    Controller.SetButtonState(Xbox360Button.Y, false);
                    Task.Delay(2000).Wait();
                    Controller.SetAxisValue(Xbox360Axis.RightThumbY, STICK_CENTER); ;
                    Controller.SetSliderValue(Xbox360Slider.RightTrigger, TRIGGER_PULLED);
                    Task.Delay(1000).Wait();
                    Controller.SetSliderValue(Xbox360Slider.RightTrigger, TRIGGER_RELEASED);
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, ":boom::white_check_mark: :3");

                    statussubtext = "Wipe Command: I hope this worked.";
                    UpdateTextDisplay();
                }
                else
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm not currently grabbing a checkpoint. That's so rude to ask me to do right now.");
                }

            }).Start();
        }


        [SlashCommand("renamecheckpoint", "I'll rename one checkpoint for another.")]
        public void SlashCommandRenameCheckpoint( //!transfercheckpoint [activity shorthand (!activities)] [(optional)master] [single word name for the checkpoint of your choosing.  a-z, 1-9 only] BungieUsername#0000
            [SlashCommandParameter(Name = "activity", Description = "the activity to launch", AutocompleteProviderType = typeof(ActivitiesWithCheckpoints))] string @activity,
            [SlashCommandParameter(Name = "master", Description = "enable master difficulty", AutocompleteProviderType = typeof(IsMasterAutoComplete))] string @masterstring,
            [SlashCommandParameter(Name = "oldname", Description = "old name of the checkpoint", AutocompleteProviderType = typeof(CheckpointAutocomplete))] string @oldname,
            [SlashCommandParameter(Name = "newname", Description = "new name of the checkpoint")] string @newname)
        {
            if (Context.Channel.Id != DiscordChannelID)
            {
                Context.Interaction.SendResponseAsync(InteractionCallback.Message("I have a designated channel I'm intended to work in. Please use that channel."));
                return;
            }
            Context.Interaction.SendResponseAsync(InteractionCallback.Message("Rename Checkpoint:"));
            if (activity == null || masterstring == null || oldname == null || newname == null)
            {
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Null value recieved, cancelling command.\nMake sure in the future you use all the available perameters.");
                return;
            }
            if (activity == "" || masterstring == "" || oldname == "" || newname == "")
            {
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Null value recieved, cancelling command.\nMake sure in the future you use all the available perameters.");
                return;
            }

            if (IsBusyWithOtherCommand()) return;

            string activitykey = activity;
            if (!CheckActivityExists("RenameCheckpoint", activity)) return;
            if (masterstring.ToLower() == "true")
            {
                activitykey = "master" + activity;
            }

            int slot = GetCharSlotOfCheckpoint(activitykey, oldname);
            if (slot == 0) return;

            newname = CleanCheckpointName(newname);
            if (newname == "")
            {
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Somehow you've managed to give me a name that when cleaned up returns an empty string\nCheckpoint names may only be letters and numbers.");
                return;
            }

            bool deleted = false;
            foreach(string key in Checkpoints[activitykey].Keys)
            {
                if(key.ToLower() == oldname.ToLower())
                {
                    Checkpoints[activitykey].Remove(key);
                    deleted = true;
                    break;
                }
            }

            if (!deleted)
            {
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I can't seem to delete that checkpoint to create a new instance of it. I'm not sure how this happened.");
                return;
            }

            Checkpoints[activitykey].Add(newname, slot);
            SaveCheckpoints();

            DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Successfully renamed " + oldname + " to " + newname + ".");
        }


        [SlashCommand("endfarm", "If a farm is running I'll verify then end it.")]
        public void SlashCommandEndFarm()
        {
            if (Context.Channel.Id != DiscordChannelID)
            {
                Context.Interaction.SendResponseAsync(InteractionCallback.Message("I have a designated channel I'm intended to work in. Please use that channel."));
                return;
            }
            Context.Interaction.SendResponseAsync(InteractionCallback.Message("End Farm:"));
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                if (VERIFYING)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm currently waiting to verify a previous command. please be patient.");
                    return;
                }

                if (!FARMMODE)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I don't believe I'm currently running a farm. If I'm mistaken please run \"!ForceRestart\" to help me find where I am.");
                    return;
                }

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I want to make sure we're on the same page. This will make me forget what I was doing and send me back to character select.\nSend \"!verify\" to confirm, or \"!cancel\" to cancel.\nIf no response is given in 60 seconds I will cancel on my own.");

                VERIFYING = true;
                VERIFYINGLEVEL = 0;

                DateTime timeout = DateTime.Now.AddMinutes(1);
                while (VERIFYINGLEVEL == 0)
                {
                    if (!VERIFYING) return;
                    if (DateTime.Now > timeout)
                    {
                        VERIFYING = false;
                        VERIFYINGLEVEL = 0;
                        DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "No valid response given in time. Continuing without returning to orbit.");
                    }
                }

                VERIFYING = false;
                FARMMODE = false;

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Ending farm and returning to character select...");

                ReturnToCharSelectFast();

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Back to idle mode...");

                UpdateStatusBar("Idle...", UserStatusType.Online);

            }).Start();
        }


        [SlashCommand("farmcheckpoint", "I'll repeatedly launch a checkpoint so that you may repeatedly farm it.")]
        public void SlashCommandFarmcheckpoint( //!transfercheckpoint [activity shorthand (!activities)] [(optional)master] [single word name for the checkpoint of your choosing.  a-z, 1-9 only] BungieUsername#0000
            [SlashCommandParameter(Name = "activity", Description = "the activity to launch", AutocompleteProviderType = typeof(ActivitiesWithCheckpoints))] string @activity,
            [SlashCommandParameter(Name = "master_feats", Description = "enable master difficulty or feats", AutocompleteProviderType = typeof(IsMasterOrFeatsAutoComplete))] string @master,
            [SlashCommandParameter(Name = "checkpoint", Description = "name of the checkpoint", AutocompleteProviderType = typeof(CheckpointAutocomplete))] string @checkpoint,
            [SlashCommandParameter(Name = "username", Description = "your bungie username including #0000")] string @bungieID
        )
        {
            if (Context.Channel.Id != DiscordChannelID)
            {
                Context.Interaction.SendResponseAsync(InteractionCallback.Message("I have a designated channel I'm intended to work in. Please use that channel."));
                return;
            }
            Context.Interaction.SendResponseAsync(InteractionCallback.Message("Farm Checkpoint:"));
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                if (VERIFYING)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm currently waiting to verify a previous command. please be patient.");
                    return;
                }
                if (activity == null || master == null || checkpoint == null || bungieID == null)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Null value recieved, cancelling command.\nMake sure in the future you use all the available perameters.");
                    return;
                }
                if (activity == "" || master == "" || checkpoint == "" || bungieID == "")
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Null value recieved, cancelling command.\nMake sure in the future you use all the available perameters.");
                    return;
                }

                if (DoubleCheckAndHandleReset()) return;
                if (IsBusyWithOtherCommand()) return;

                statusheader = "!FarmCheckpoint command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                UpdateStatusBar("!FarmCheckpoint... Validating command.", UserStatusType.Idle);

                FARMMODE = true;

                WorkingDiscordName = Context.User.Username;
                if (Context.User.GlobalName != null) WorkingDiscordName = Context.User.GlobalName;

                while (AFKCYCLE)
                {
                }

                string standardizedcommand = "deadspace " + activity + " ";
                if (master.ToLower() == "true") standardizedcommand = standardizedcommand + "master ";
                if (master.Contains("feats:")) standardizedcommand = standardizedcommand + master;
                standardizedcommand = standardizedcommand + checkpoint + " " + bungieID;

                CommandLayout output = ParseStandardCommand(standardizedcommand, "FarmCheckpoint", true);
                if (!output.works)
                {
                    FARMMODE = false;
                    return;
                }
                WorkingUserName = output.guardianname;

                int charslot = GetCharSlotOfCheckpoint(output.activitykey, output.checkpointname);
                if (charslot == 0)
                {
                    FARMMODE = false;
                    return;
                }

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Preparing farm :3");

                while (FARMMODE)
                {
                    if (!FARMMODE) //im already on character select if this triggers here
                    {
                        UpdateStatusBar("Idle...", UserStatusType.Online);
                        return;
                    }

                    NavigateToActivityFromCharSelect(charslot, output.activity, output.master);
                    if (!FARMMODE) break;

                    if (output.activitykey == "DP" || output.activitykey == "EDP" || output.activitykey == "EQ") ClearFeats();
                    if (output.feats) SelectFeats(output.featlist);
                    if (!FARMMODE) break;

                    InvitePlayer("/invite " + WorkingUserName);

                    statussubtext = "Invite sent. Waiting for player to join.";
                    UpdateTextDisplay();

                    UpdateStatusBar("!FarmCheckpoint... Invite sent to " + WorkingUserName + "... Awaiting their arrival.", UserStatusType.Idle);

                    Point pointcheck = ConvertAspectRatioCoords(95.859, 83.75);
                    Point pointclick = ConvertAspectRatioCoords(75.117, 83.75);

                    SetCursorPos(pointclick.X, pointclick.Y);

                    Task.Delay(1000).Wait();
                    if (!FARMMODE) break;

                    bool joined = WaitForPlayerJoinOrbit(DateTime.Now.AddMinutes(55));
                    if (!joined)
                    {
                        FARMMODE = false;
                        break;
                    }

                    UpdateStatusBar("!FarmCheckpoint... Join detected, launching momentarily.", UserStatusType.Idle);
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Launching activity as soon as it lets me...");
                    statussubtext = "Join detected. Launching activity.";
                    UpdateTextDisplay();

                    Task.Delay(2000).Wait();

                    SpamLaunchButtonUntilWorks();

                    statussubtext = "Awaiting first black screen.";
                    UpdateTextDisplay();
                    DateTime bailout = DateTime.Now.AddSeconds(30);
                    while (!CheckBlackScreen())
                    {
                        if (DateTime.Now > bailout) break;
                        Task.Delay(30).Wait();
                    }

                    UpdateStatusBar("!FarmCheckpoint... Returning to orbit to prep again.", UserStatusType.Idle);
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Returning to orbit to begin the cycle again.");
                    ReturnToCharSelectFast();
                }

                ReturnToCharSelectFast();
                UpdateStatusBar("Idle...", UserStatusType.Online);
            }).Start();
        }


        [SlashCommand("listcheckpoints", "I'll list all available checkpoints.")]
        public void SlashCommandListCheckpoints()
        {
            if (Context.Channel.Id != DiscordChannelID)
            {
                Context.Interaction.SendResponseAsync(InteractionCallback.Message("I have a designated channel I'm intended to work in. Please use that channel."));
                return;
            }
            Context.Interaction.SendResponseAsync(InteractionCallback.Message("List Checkpoints:"));
            //make sure reset hasnt happened yet.
            if (DoubleCheckAndHandleReset()) return;

            //list out all checkpoints if no activity is specified. otherwise, list all checkpoints for the given activity. If there are none, explain that.
            string outputraid = "## Raids:\n";
            string outputdungeon = "## Dungeons:\n";
            string outputpantheon = "## Pantheon:\n";
            bool good = false;
            foreach (string key in Checkpoints.Keys)
            {
                if (Checkpoints[key].Count > 0)
                {
                    good = true;

                    string checkstr = "";
                    int i = 1;
                    foreach (string checkpointname in Checkpoints[key].Keys)
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

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, outputraid).Wait();
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, outputdungeon).Wait();
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, outputpantheon).Wait();
            }
            else
            {
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I currently have no checkpoints. :(");
            }
        }


        [SlashCommand("deletecheckpoint", "I'll delete a checkpoint to be replaced with something else.")]
        public void SlashCommandDeleteCheckpoint( //!transfercheckpoint [activity shorthand (!activities)] [(optional)master] [single word name for the checkpoint of your choosing.  a-z, 1-9 only] BungieUsername#0000
            [SlashCommandParameter(Name = "activity", Description = "the activity to launch", AutocompleteProviderType = typeof(ActivitiesWithCheckpoints))] string @activity,
            [SlashCommandParameter(Name = "master", Description = "enable master difficulty", AutocompleteProviderType = typeof(IsMasterAutoComplete))] string @master,
            [SlashCommandParameter(Name = "checkpoint", Description = "name of the checkpoint", AutocompleteProviderType = typeof(CheckpointAutocomplete))] string @checkpoint
        )
        {
            if (Context.Channel.Id != DiscordChannelID)
            {
                Context.Interaction.SendResponseAsync(InteractionCallback.Message("I have a designated channel I'm intended to work in. Please use that channel."));
                return;
            }
            Context.Interaction.SendResponseAsync(InteractionCallback.Message("Delete Checkpoint:"));
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                if (VERIFYING)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm currently waiting to verify a previous command. please be patient.");
                    return;
                }
                if (activity == null || master == null || checkpoint == null)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Null value recieved, cancelling command.\nMake sure in the future you use all the available perameters.");
                    return;
                }
                if (activity == "" || master == "" || checkpoint == "")
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Null value recieved, cancelling command.\nMake sure in the future you use all the available perameters.");
                    return;
                }

                if (DoubleCheckAndHandleReset()) return;
                if (IsBusyWithOtherCommand()) return;

                statusheader = "!DeleteCheckpoint command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                UpdateStatusBar("!DeleteCheckpoint... Validating command.", UserStatusType.Idle);

                DELETINGCHECKPOINT = true;

                WorkingDiscordName = Context.User.Username;
                if (Context.User.GlobalName != null) WorkingDiscordName = Context.User.GlobalName;

                while (AFKCYCLE)
                {
                }

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Making sure I have the checkpoint and everything is correct...");

                //!DeleteCheckpoint [activity shorthand (!activities)] [(optional)master] [single word name of the checkpoint. a-z, 0-9 only]

                string standardizedcommand = "deadspace " + activity + " ";
                if (master.ToLower() == "true") standardizedcommand = standardizedcommand + "master ";
                standardizedcommand = standardizedcommand + checkpoint;
                //parse command text
                CommandLayout output = ParseStandardCommand(standardizedcommand, "DeleteCheckpoint", false);
                if (!output.works)
                {
                    DELETINGCHECKPOINT = false;
                    return;
                }

                int charslot = GetCharSlotOfCheckpoint(output.activitykey, output.checkpointname);
                if (charslot == 0)
                {
                    DELETINGCHECKPOINT = false;
                    return;
                }

                UpdateStatusBar("!DeleteCheckpoint... Navigating to activity.", UserStatusType.Idle);

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Command acknowledged, navigating to the activity to delete the checkpoint.");

                NavigateToActivityFromCharSelect(charslot, output.activity, output.master);

                UpdateStatusBar("!DeleteCheckpoint... Removing checkpoint.", UserStatusType.Idle);
                RemoveCheckpoint();

                bool deleted = false;
                foreach (string key in Checkpoints[output.activitykey].Keys)
                {
                    if (key.ToLower() == output.checkpointname.ToLower())
                    {
                        Checkpoints[output.checkpointname].Remove(key);
                        deleted = true;
                        break;
                    }
                }

                if (!deleted)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I can't seem to delete that checkpoint, despite knowing I have it. I'm not sure how this happened.");
                    return;
                }

                SaveCheckpoints();

                UpdateStatusBar("!DeleteCheckpoint... Returning to character select...", UserStatusType.Idle);
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Checkpoint deleted, I'm returning to orbit now.");

                ReturnToCharSelectFast();
                DELETINGCHECKPOINT = false;

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Idling...");
                UpdateStatusBar("Idle...", UserStatusType.Online);
            }).Start();
        }


        [SlashCommand("endhold", "If I'm holding a checkpoint I'll stop.")]
        public void SlashCommandEndhold()
        {
            if (Context.Channel.Id != DiscordChannelID)
            {
                Context.Interaction.SendResponseAsync(InteractionCallback.Message("I have a designated channel I'm intended to work in. Please use that channel."));
                return;
            }
            Context.Interaction.SendResponseAsync(InteractionCallback.Message("End Hold:"));
            //check to see if im even holdling a checkpoint. 
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                //verify.
                VERIFYING = true;
                VERIFYINGLEVEL = 0;

                string oldstatus = statusheader;
                string oldsubtext = statussubtext;

                statusheader = "Processing !EndHold command:";
                statussubtext = "Awaiting first verification.";
                UpdateTextDisplay();

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Are you sure you're done holding the load? If you're doing this to a load someone else is holding please make sure they're done.\nSend \"!verify\" to confirm or \"!cancel\" to cancel. If no message is sent in 60 seconds I'll go back to idleing.");

                DateTime timeout = DateTime.Now.AddMinutes(1);
                while (VERIFYINGLEVEL == 0)
                {
                    if (!VERIFYING) return;
                    if (DateTime.Now > timeout)
                    {
                        VERIFYING = false;
                        VERIFYINGLEVEL = 0;
                        DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "No valid response given in time. Continuing without returning to orbit.");

                        statusheader = oldstatus;
                        statussubtext = oldsubtext;
                        UpdateTextDisplay();
                    }
                }
                VERIFYING = false;
                HOLDINGLOAD = false;
            }).Start();
        }


        [SlashCommand("verify", "Verifies a previous command.")]
        public void SlashCommandVerify()
        {
            if (Context.Channel.Id != DiscordChannelID)
            {
                Context.Interaction.SendResponseAsync(InteractionCallback.Message("I have a designated channel I'm intended to work in. Please use that channel."));
                return;
            }
            Context.Interaction.SendResponseAsync(InteractionCallback.Message("Verify Recieved."));
            if (VERIFYING) VERIFYINGLEVEL++;
            else
            {
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm not currently verifying a command...");
            }
        }


        [SlashCommand("holdload", "I'll join on the attached username, and spam them with invites.")]
        public void SlashCommandHoldLoad([SlashCommandParameter(Name = "username", Description = "your bungie username including #0000")] string @bungieID)
        {
            if (Context.Channel.Id != DiscordChannelID)
            {
                Context.Interaction.SendResponseAsync(InteractionCallback.Message("I have a designated channel I'm intended to work in. Please use that channel."));
                return;
            }
            Context.Interaction.SendResponseAsync(InteractionCallback.Message("Hold Load:"));
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                if (VERIFYING)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm currently waiting to verify a previous command. please be patient.");
                    return;
                }
                if (bungieID == null)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Null value recieved, cancelling command.\nMake sure in the future you use all the available perameters.");
                    return;
                }
                if (bungieID == "")
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Null value recieved, cancelling command.\nMake sure in the future you use all the available perameters.");
                    return;
                }

                if (IsBusyWithOtherCommand()) return;

                statusheader = "!HoldLoad command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                UpdateStatusBar("!HoldLoad... Validating command.", UserStatusType.Idle);

                HOLDINGLOAD = true;

                WorkingDiscordName = Context.User.Username;
                if (Context.User.GlobalName != null) WorkingDiscordName = Context.User.GlobalName;

                while (AFKCYCLE)
                {
                }

                string[] nameID = bungieID.Split("#");
                if (nameID.Length == 1 || nameID.Last().Split('#').Last().Length != 4)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "For the \"!holdload\" command to work, I need the 4 number hashtag after your guardians name.\nTry \"!help holdload\" to learn how to use it.");
                    HOLDINGLOAD = false;

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                    return;
                }

                WorkingUserName = bungieID;

                int charslot = 1;

                //activity for the activity name (shorthand + master)
                //charslot for which character the checkpoint is stored on.
                //master (bool) to know if its master mode or not

                statusheader = "!HoldLoad command:";
                statussubtext = "Attempting to join...";
                UpdateTextDisplay();

                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Checking to see if I can join...");
                SelectChar(charslot);

                UpdateStatusBar("!HoldLoad... Joining fireteam.", UserStatusType.Idle);
                bool worked = JoinFireteamFromOrbit("/join " + WorkingUserName, "");
                if (!worked)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Looks like your fireteam is currently unavailable. Returning to idling.");
                    HOLDINGLOAD = false;

                    if (CheckIfOrbitTextBox() != "")
                    {
                        //make sure im in controller mode
                        Controller.SetButtonState(Xbox360Button.RightThumb, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, false);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, false);
                        Task.Delay(101).Wait();

                        Controller.SetButtonState(Xbox360Button.B, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.B, false);
                        Task.Delay(101).Wait();
                    }

                    ReturnToCharSelectFast();

                    UpdateStatusBar("Idle...", UserStatusType.Online);

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();
                }
                else
                {

                    UpdateStatusBar("!HoldLoad... Currently holding load for " + WorkingDiscordName + ". Run !endhold to stop.", UserStatusType.Idle);
                    statusheader = "!HoldLoad command:";
                    statussubtext = "Boots on ground. Going to AFK macro.";
                    UpdateTextDisplay();

                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Now boots on the ground and holding the load. Remember to run \"!endhold\" when you want me to stop.");

                    Task.Delay(10000).Wait();

                    VerifyControllerInput();
                    NavigateToCollections();

                    while (HOLDINGLOAD)
                    {
                        if (!SingleAfkCycle()) break;

                        SendClick(new Point(50, 50));
                        InvitePlayer("/invite " + WorkingUserName);
                        Task.Delay(2000).Wait();

                        VerifyControllerInput();
                        Task.Delay(10000).Wait();
                    }

                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Endhold command processed. Returning to orbit...");

                    Controller.SetButtonState(Xbox360Button.B, true);
                    Task.Delay(200).Wait();
                    Controller.SetButtonState(Xbox360Button.B, false);
                    Task.Delay(500).Wait();
                    Controller.SetButtonState(Xbox360Button.B, true);
                    Task.Delay(200).Wait();
                    Controller.SetButtonState(Xbox360Button.B, false);
                    Task.Delay(500).Wait();
                    Controller.SetButtonState(Xbox360Button.B, true);
                    Task.Delay(200).Wait();
                    Controller.SetButtonState(Xbox360Button.B, false);
                    Task.Delay(500).Wait();

                    ReturnToCharSelectFast();

                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Idling...");

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                }
            }).Start();
        }


        [SlashCommand("grabcheckpoint", "I'll join and wait for a wipe to get a checkpoint from you.")]
        public void SlashCommandGrabCheckpoint( //!transfercheckpoint [activity shorthand (!activities)] [(optional)master] [single word name for the checkpoint of your choosing.  a-z, 1-9 only] BungieUsername#0000
            [SlashCommandParameter(Name = "activity", Description = "the activity to launch", ChoicesProviderType = typeof(ActivityProvider))] string @activity,
            [SlashCommandParameter(Name = "master", Description = "enable master difficulty", AutocompleteProviderType = typeof(CanMaster))] string @master,
            [SlashCommandParameter(Name = "checkpoint", Description = "single word name of the checkpoint")] string @checkpoint,
            [SlashCommandParameter(Name = "username", Description = "your bungie username including #0000")] string @bungieID
        )
        {
            if (Context.Channel.Id != DiscordChannelID)
            {
                Context.Interaction.SendResponseAsync(InteractionCallback.Message("I have a designated channel I'm intended to work in. Please use that channel."));
                return;
            }
            Context.Interaction.SendResponseAsync(InteractionCallback.Message("Grab Checkpoint:"));
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                if (VERIFYING)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm currently waiting to verify a previous command. please be patient.");
                    return;
                }
                if (activity == null || master == null || checkpoint == null || bungieID == null)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Null value recieved, cancelling command.\nMake sure in the future you use all the available perameters.");
                    return;
                }
                if (activity == "" || master == "" || checkpoint == "" || bungieID == "")
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Null value recieved, cancelling command.\nMake sure in the future you use all the available perameters.");
                    return;
                }

                //make sure reset hasnt happened yet.

                if (DoubleCheckAndHandleReset()) return;
                if (IsBusyWithOtherCommand()) return;

                statusheader = "!GrabCheckpoint command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                UpdateStatusBar("!GrabCheckpoint... Validating command.", UserStatusType.Idle);

                GRABBINGCHECKPOINT = true;

                WorkingDiscordName = Context.User.Username;
                if (Context.User.GlobalName != null) WorkingDiscordName = Context.User.GlobalName;

                while (AFKCYCLE)
                {
                }

                string standardizedcommand = "deadspace " + activity + " ";
                if (master.ToLower() == "true") standardizedcommand = standardizedcommand + "master ";
                standardizedcommand = standardizedcommand + checkpoint + " " + bungieID;

                CommandLayout output = ParseStandardCommand(standardizedcommand, "GrabCheckpoint", true);
                if (!output.works)
                {
                    GRABBINGCHECKPOINT = false;
                    return;
                }

                GRABBINGCHECKPOINT = CheckOverlappingCheckpointName(output.checkpointname, output.activitykey, "FlyInCheckpointTransfer");
                if (!GRABBINGCHECKPOINT) return;
                GRABBINGCHECKPOINT = CheckCheckpointsFull(output.activitykey);
                if (!GRABBINGCHECKPOINT) return;

                int charslot = GetFirstEmptyCharSlot(output.activitykey);
                WorkingUserName = output.guardianname;

                statusheader = "!GrabCheckpoint command:";
                statussubtext = "Attempting to join...";
                UpdateTextDisplay();
                UpdateStatusBar("!GrabCheckpoint... Attempting to join " + WorkingUserName + ".", UserStatusType.Idle);
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Attempting to join...");

                SelectChar(charslot);
                bool worked = JoinFireteamFromOrbit("/join " + WorkingUserName, output.activity);
                if (!worked)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Looks like your fireteam is currently unavailable. Returning to idling.");
                    GRABBINGCHECKPOINT = false;

                    if (CheckIfOrbitTextBox() != "")
                    {
                        //make sure im in controller mode
                        Controller.SetButtonState(Xbox360Button.RightThumb, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, false);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, false);
                        Task.Delay(101).Wait();

                        Controller.SetButtonState(Xbox360Button.B, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.B, false);
                        Task.Delay(101).Wait();
                    }

                    ReturnToCharSelectFast();

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                }
                else
                {
                    //wait for wipe and then return to orbit

                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm now pretty sure I'm boots on ground. Wipe when ready.");

                    statusheader = "!GrabCheckpoint command:";
                    statussubtext = "Boots on ground. Awaiting wipe screen.";
                    UpdateTextDisplay();
                    UpdateStatusBar("!GrabCheckpoint... Awaiting wipe :3", UserStatusType.Idle);

                    AwaitText("lightfadesaway", ConvertAspectRatioCoords(30.98958333333, 9.25925925925926), ConvertAspectRatioCoords(66.927083333, 15.55555555555));

                    statusheader = "!GrabCheckpoint command:";
                    statussubtext = "Wipe screen found. Waiting for wipe screen to clear.";
                    UpdateTextDisplay();

                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Wipe detected. As soon as the wipe screen closes I'll return to orbit.");

                    AwaitColorChange(50, 50, 2);

                    if (!output.master) DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Checkpoint " + output.checkpointname + " grabbed for " + output.activity + " from " + WorkingUserName + ".\n Returning to orbit to idle.");
                    else DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Checkpoint " + output.checkpointname + " grabbed for master " + output.activity + " from " + WorkingUserName + ".\n Returning to orbit to idle.");

                    Checkpoints[output.activitykey].Add(output.checkpointname, charslot);
                    SaveCheckpoints();

                    GRABBINGCHECKPOINT = false;
                    flagBootsOnGround = false;

                    UpdateStatusBar("!GrabCheckpoint... Returning to orbit.", UserStatusType.Idle);
                    ReturnToCharSelectFast();

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);
                }
            }).Start();
        }


        [SlashCommand("grabcheckpointandconfirm", "I'll join and wait for the endhold command to get a checkpoint from you.")]
        public void SlashCommandGrabCheckpointAndConfirm( //!transfercheckpoint [activity shorthand (!activities)] [(optional)master] [single word name for the checkpoint of your choosing.  a-z, 1-9 only] BungieUsername#0000
            [SlashCommandParameter(Name = "activity", Description = "the activity to launch", ChoicesProviderType = typeof(ActivityProvider))] string @activity,
            [SlashCommandParameter(Name = "master", Description = "enable master difficulty", AutocompleteProviderType = typeof(CanMaster))] string @master,
            [SlashCommandParameter(Name = "checkpoint", Description = "single word name of the checkpoint")] string @checkpoint,
            [SlashCommandParameter(Name = "username", Description = "your bungie username including #0000")] string @bungieID
        )
        {
            if (Context.Channel.Id != DiscordChannelID)
            {
                Context.Interaction.SendResponseAsync(InteractionCallback.Message("I have a designated channel I'm intended to work in. Please use that channel."));
                return;
            }
            Context.Interaction.SendResponseAsync(InteractionCallback.Message("Grab Checkpoint and Confirm:"));
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                if (VERIFYING)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm currently waiting to verify a previous command. please be patient.");
                    return;
                }
                if (activity == null || master == null || checkpoint == null || bungieID == null)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Null value recieved, cancelling command.\nMake sure in the future you use all the available perameters.");
                    return;
                }
                if (activity == "" || master == "" || checkpoint == "" || bungieID == "")
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Null value recieved, cancelling command.\nMake sure in the future you use all the available perameters.");
                    return;
                }

                if (DoubleCheckAndHandleReset()) return;
                if (IsBusyWithOtherCommand()) return;

                statusheader = "!GrabCheckpointAndConfirm command:";
                statussubtext = "Making sure the command is viable";
                UpdateTextDisplay();

                UpdateStatusBar("!GrabCheckpointAndConfirm... Validating command.", UserStatusType.Idle);

                GRABBINGCHECKPOINT = true;

                WorkingDiscordName = Context.User.Username;
                if (Context.User.GlobalName != null) WorkingDiscordName = Context.User.GlobalName;

                while (AFKCYCLE)
                {
                }

                string standardizedcommand = "deadspace " + activity + " ";
                if (master.ToLower() == "true") standardizedcommand = standardizedcommand + "master ";
                standardizedcommand = standardizedcommand + checkpoint + " " + bungieID;

                CommandLayout output = ParseStandardCommand(standardizedcommand, "GrabCheckpointAndConfirm", true);
                if (!output.works)
                {
                    GRABBINGCHECKPOINT = false;
                    return;
                }

                GRABBINGCHECKPOINT = CheckOverlappingCheckpointName(output.checkpointname, output.activitykey, "FlyInCheckpointTransfer");
                if (!GRABBINGCHECKPOINT) return;
                GRABBINGCHECKPOINT = CheckCheckpointsFull(output.activitykey);
                if (!GRABBINGCHECKPOINT) return;

                int charslot = GetFirstEmptyCharSlot(output.activitykey);
                WorkingUserName = output.guardianname;

                statusheader = "!GrabCheckpointAndConfirm command:";
                statussubtext = "Attempting to join...";
                UpdateTextDisplay();
                UpdateStatusBar("!GrabCheckpointAndConfirm... Attempting to join " + WorkingUserName + ".", UserStatusType.Idle);
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Attempting to join...");

                SelectChar(charslot);
                bool worked = JoinFireteamFromOrbit("/join " + WorkingUserName, output.activity);
                if (!worked)
                {
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Looks like your fireteam is currently unavailable. Returning to idling.");
                    GRABBINGCHECKPOINT = false;

                    if (CheckIfOrbitTextBox() != "")
                    {
                        //make sure im in controller mode
                        Controller.SetButtonState(Xbox360Button.RightThumb, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, false);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.RightThumb, false);
                        Task.Delay(101).Wait();

                        Controller.SetButtonState(Xbox360Button.B, true);
                        Task.Delay(101).Wait();
                        Controller.SetButtonState(Xbox360Button.B, false);
                        Task.Delay(101).Wait();
                    }

                    ReturnToCharSelectFast();

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();
                    UpdateStatusBar("Idle...", UserStatusType.Online);
                }
                else
                {
                    //make sure its not an error code please TODO - not sure how to synthesize an error code here honestly.

                    //wait for wipe and then return to orbit

                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm now pretty sure I'm boots on ground. Let me know when to leave with \"!endhold\".");

                    HOLDINGLOAD = true;

                    Task.Delay(10000).Wait();

                    VerifyControllerInput();
                    NavigateToCollections();

                    while (HOLDINGLOAD)
                    {
                        SingleAfkCycle();
                    }

                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "Endhold command processed. Returning to orbit...");

                    Controller.SetButtonState(Xbox360Button.B, true);
                    Task.Delay(200).Wait();
                    Controller.SetButtonState(Xbox360Button.B, false);
                    Task.Delay(500).Wait();
                    Controller.SetButtonState(Xbox360Button.B, true);
                    Task.Delay(200).Wait();
                    Controller.SetButtonState(Xbox360Button.B, false);
                    Task.Delay(500).Wait();
                    Controller.SetButtonState(Xbox360Button.B, true);
                    Task.Delay(200).Wait();
                    Controller.SetButtonState(Xbox360Button.B, false);

                    Task.Delay(500).Wait(); UpdateStatusBar("!GrabCheckpointAndConfirm: Making sure the checkpoint saved correctly.", UserStatusType.Idle);

                    statusheader = "!GrabCheckpointAndConfirm command:";
                    statussubtext = "Boots on ground. Returning to orbit.";
                    UpdateTextDisplay();

                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "I'm now double checking to make sure the checkpoint saved correctly.");

                    //return to char select, then see if the checkpoint saved.
                    ReturnToCharSelectFast();

                    VerifyCheckpointAndSave(charslot, output);

                    GRABBINGCHECKPOINT = false;
                    flagBootsOnGround = false;
                    ReturnToCharSelectFast();

                    statusheader = "Idle...";
                    statussubtext = "";
                    UpdateTextDisplay();

                    UpdateStatusBar("Idle...", UserStatusType.Online);

                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "GrabCheckpointAndConfirm complete. Back to idling...");
                }
            }).Start();
        }


        [SlashCommand("listactivities", "Lists all activities as their shorthand form.")]
        public void SlashCommandActivities()
        {
            if (Context.Channel.Id != DiscordChannelID)
            {
                Context.Interaction.SendResponseAsync(InteractionCallback.Message("I have a designated channel I'm intended to work in. Please use that channel."));
                return;
            }
            Context.Interaction.SendResponseAsync(InteractionCallback.Message("List Activities:\n## Raids:\n" +
                " - CE (Crota's End) \n" +
                " - DSC (Deep Stone Crypt) \n" +
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
                " - GAUNTLET (Full 7 boss pantheon)")).Wait();
        }


        [SlashCommand("listfeats", "Lists all feats as I recognise them.")]
        public void SlashCommandFeats()
        {
            if (Context.Channel.Id != DiscordChannelID)
            {
                Context.Interaction.SendResponseAsync(InteractionCallback.Message("I have a designated channel I'm intended to work in. Please use that channel."));
                return;
            }
            Context.Interaction.SendResponseAsync(InteractionCallback.Message("List Feats:"));
            //Token, Phase, Battalions, Challenges, and Cutthroat.
            DiscordClient.Rest.SendMessageAsync(DiscordChannelID, "## Feats:\n" +
                " - Token (Token Limit) \n" +
                " - Phase (Phase Limit) \n" +
                " - Battalions \n" +
                " - Challenges (Encounter Challenges) \n" +
                " - Cutthroat (Cutthroat Combat)");
        }


        [SlashCommand("listcommands", "Lists all available standard commands and brief descriptions.")]
        public void SlashCommandListCommands()
        {
            Context.Interaction.SendResponseAsync(InteractionCallback.Message("List Commands:"));
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            DiscordClient.Rest.SendMessageAsync(DiscordChannelID,
                "## Available commands:\n" +
                " - **!Help:** !Help [command] to see a MUCH more detailed description of any of these commands, and how to use them.\n" +
                " - **!ListCommands:** I'll list all available commands and brief definitions.\n" +
                " - **!Activities:** I'll list all available activities.\n" +
                " - **!HoldLoad:** I'll hold a load by joining on the person until the !EndHold command is run. Good for grabbing bonus chests.\n" +
                " - **!EndHold:** Stops me holding a load.\n" +
                " - **!GrabCheckpoint:** Has me join on the username given with the command, and then wait for a wipe so that I have the checkpoint, at which point I'll then return to orbit and idle.\n" +
                " - **!ForceWipe:** If applicable, I'll fire a rocket at the ground to force a wipe.\n" +
                " - **!LaunchAndHold:** I'll launch a checkpoint and afk on the ground, occasionally attempting to send an invite to the author so that people may grab bonus chests from a checkpoint.\n" +
                " - **!DeleteCheckpoint:** I'll delete a checkpoint so that it may be replaced with another.\n").Wait();
            DiscordClient.Rest.SendMessageAsync(DiscordChannelID,
                " - **!ListCheckpoints:** Used to list what checkpoints I have. \n" +
                " - **!ListFeats:** Used to list what feats are available. \n" +
                " - **!FarmCheckpoint:** Used to target farm a specific encounter.\n" +
                " - **!EndFarm:** I'll stop farming the given activity and shift into idle mode.\n" +
                " - **!TransferCheckpoint:** Used to transfer a checkpoint from me to you.\n" +
                " - **!FlyInCheckpointTransfer:** Allows giving checkpoints to me without the use of a darkness zone or wipe. warning: complicated.\n" +
                " - **!GrabCheckpointAndConfirm:** I'll join on you and then afk until you tell me I should have a checkpoint. Then I'll check if I've aquired a new checkpoint once I bail and echo the results back.\n" +
                " - **!RenameCheckpoint:** Renames a checkpoint.\n" +
                " - **!CleanCheckpoints:** Takes half an hour, I'll go thru and clean out any vestigial checkpoints I have no memory of.\n" +
                " - **!ForceRestart:** Shuts down the entire bot and restarts it. Useful if I'm bugging out.\n");

#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }


        [SlashCommand("help", "Get detailed information for specific commands.")]
        public void SlashCommandHelp([SlashCommandParameter(Name = "command", Description = "The command you want help with",ChoicesProviderType = typeof(CommandList))] string @command)
        {
            if (Context.Channel.Id != DiscordChannelID)
            {
                Context.Interaction.SendResponseAsync(InteractionCallback.Message("I have a designated channel I'm intended to work in. Please use that channel."));
                return;
            }
            Context.Interaction.SendResponseAsync(InteractionCallback.Message("Help:"));
            //else, check if the command exists. if the command exists, give its specific description.
            bool done = false;
            switch (command.ToLower())
            {
                case "listcommands":
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID,
                        "### !ListCommands:\n" +
                        " - I'll list all available commands and shorthand definitions of them.\n" +
                        " - Usage: !ListCommands\n");
                    done = true;
                    return;
                case "launchandhold":
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID,
                        "### !LaunchAndHold \n" +
                        " - Has me load into a checkpoint and stay boots on the ground until the \"!endhold\" command is run. I will occasionally send invites to the person who ran the command so they may rejoin.\n" +
                        " - Usage: !LaunchAndHold [activity shorthand(!activities)] [(optional)master] [single word name given to the checkpoint.  a-z, 0-9 only] BungieUsername#0000");
                    done = true;
                    return;
                case "activities":
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID,
                    "### !Activities\n" +
                    " - I'll list all available activities.\n" +
                    " - usage: !Activities\n");
                    done = true;
                    return;
                case "holdload":
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID,
                    "### !HoldLoad: \n" +
                    " - I'll hold a load by joining on the person until the !EndHold command is run.\n" +
                    " - usage: !HoldLoad BungieUsername#0000\n");
                    done = true;
                    return;
                case "endhold":
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID,
                        "### !EndHold: \n" +
                        " - First I will confirm that you do want to end the hold, with !verify.\n" +
                        " - After that, I'll return to orbit and enter idle mode.\n" +
                        " - usage: !EndHold \n");
                    done = true;
                    return;
                case "grabcheckpoint":
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID,
                        "### !GrabCheckpoint: \n" +
                        " - Has me join on the username given with the command, and then wait for a wipe so that I have the checkpoint, at which point I'll then return to orbit and idle.\n" +
                        " - usage: !GrabCheckpoint [activity shorthand (!activities)] [(optional)master] [single word name for the checkpoint of your choosing.  a-z, 1-9 only] BungieUsername#0000 \n" +
                        " - example: !GrabCheckpoint WR ogre ItAvvy#7006\n" +
                        " - If I already have a checkpoint in that activity on all 3 characters I will list them and ask you to delete one using !deletecheckpoint.");
                    done = true;
                    return;
                case "grabcheckpointandconfirm":
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID,
                        "### !GrabCheckpointAndConfirm: \n" +
                        " - Has me join on the username given with the command, and then wait for confirmation that I should leave, at which point I'll then return to orbit and confirm that I got the checkpoint.\n" +
                        " - When you're ready for me to go confirm that I have the checkpoint run the \"!endhold\" command.\n" +
                        " - usage: !GrabCheckpointAndConfirm [activity shorthand (!activities)] [(optional)master] [single word name for the checkpoint of your choosing.  a-z, 1-9 only] BungieUsername#0000 \n" +
                        " - example: !GrabCheckpointAndConfirm WR ogre ItAvvy#7006\n" +
                        " - If I already have a checkpoint in that activity on all 3 characters I will list them and ask you to delete one using !deletecheckpoint.");
                    done = true;
                    return;
                case "deletecheckpoint":
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID,
                        "### !DeleteCheckpoint:\n" +
                        " - I'll delete a checkpoint from a given activity with a given name so that a new checkpoint may be gotten on that character.\n" +
                        " - usage: !DeleteCheckpoint [activity shorthand (!activities)] [(optional)master] [single word name of the checkpoint. a-z, 0-9 only] \n" +
                        " - example: !DeleteCheckpoint WR master ogre");
                    done = true;
                    return;
                case "listcheckpoints":
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID,
                        "### !ListCheckpoints: \n" +
                        " - Lists out all checkpoints, and specifies master in cases where its applicable.\n" +
                        " - usage: !ListCheckpoints");
                    done = true;
                    return;
                case "listfeats":
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID,
                        "### !ListFeats: \n" +
                        " - Lists out all feats as I recognise them.\n" +
                        " - usage: !ListFeats");
                    done = true;
                    return;
                case "farmcheckpoint":
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID,
                        "### !FarmCheckpoint: \n" +
                        " - I'll load the character the given checkpoint is on, and I'll wait in orbit for you to join. The moment you join I'll launch the activity, transferring the checkpoint on load-in. Then, I will return to orbit to wait to launch again. Use !EndFarm to end the farm. If you specify feats with the optional modifier, I will try to launch the checkpoint with those feats if applicable.\n" +
                        " - Viable Feats: Token, Phase, Battalions, Challenges, and Cutthroat. \n" +
                        " - usage: !FarmCheckpoint [activity shorthand (!activities)] [(optional)master] [(optional)feats:feat1name,feat2name,etc...] [single word name for the checkpoint of your choosing. a-z, 0-9 only]  BungieUsername#0000 \n" +
                        " - example: !FarmCheckpoint EQ feats:tokenlimit,phaselimit shockyhands ItAvvy#7006");
                    done = true;
                    return;
                case "endfarm":
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID,
                        "### !EndFarm: \n" +
                        " - I'll stop farming the given activity and shift into idle mode. I will then ask for you to run !verify to verify that you do in fact want to end the farm.\n" +
                        " - Usage: !EndFarm");
                    done = true;
                    return;
                case "forcewipe":
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID,
                        "### !ForceWipe: \n" +
                        " - If applicable, I'll fire a rocket at the ground to force a wipe.\n" +
                        " - usage: !ForceWipe");
                    done = true;
                    return;
                case "help":
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID,
                        "### !Help: \n" +
                        " - Why are you asking for help with the \"help\" command???\n" +
                        " - usage: !Help [command name]");
                    done = true;
                    return;
                case "transfercheckpoint":
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID,
                        "### !TransferCheckpoint \n" +
                        " - Has me load into a checkpoint only a single time after someone joins my lobby, so that I may transfer the checkpoint to them, and then I'll return to idle.\n" +
                        " - Usage: !TransferCheckpoint [activity shorthand(!activities)] [(optional)master] [single word name given to the checkpoint.  a-z, 0-9 only] BungieUsername#0000");
                    done = true;
                    return;
                case "flyincheckpointtransfer":
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID,
                        "### !FlyInCheckpointTransfer\n" +
                        " - Transfers a checkpoint from you, to me, the hard way without using a darkness zone.\n" +
                        " - Warning, this requires a bit of cooperation, and will only work if your fireteam is set to open.\n" +
                        " - First, navigate on your director to the activity that has the checkpoint you want to transfer on it, and wait in orbit. Then run this command.\n" +
                        " - After you run the command I will attempt to join on you.\n" +
                        " - Then, I'll ask you to launch the activity, open your inventory, navigate to \"change character\" in your settings, click it, and then have you wait to confirm.\n" +
                        " - Once my screen goes black, I'll send a chat message telling you to hit confirm to change characters.\n" +
                        " - Once I'm boots on the ground I will return to orbit, verify that I do have the checkpoint, and echo the result here.\n" +
                        " - Usage: !flyincheckpointtransfer [activity shorthand (!activities)] [(optional)master] [single word name of the checkpoint.  a-z, 1-9 only] BungieUsername#0000");
                    done = true;
                    return;
                case "cleancheckpoints":
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID,
                        "### !CleanCheckpoints\n" +
                        " - I will go thru, activity by activity, both normal and master and delete any erronious checkpoints I may have that I don't have record of.\n" +
                        " - This does require you to verify that you want to do it beforehand, as it takes about 30 minutes to go thru everything.\n" +
                        " - Usage: !CleanCheckpoints");
                    done = true;
                    return;
                case "forcerestart":
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID,
                        "### !ForceRestart\n" +
                        " - I will first verify you want to do this. Twice.\n" +
                        " - After verifying, I will kill the d2 process, and then restart myself to have a \"blank slate\" so to speak, and to unstuck myself.\n" +
                        " - This __DOES NOT__ delete any checkpoints.\n" +
                        " - Usage: !ForceRestart");
                    done = true;
                    return;
                case "renamecheckpoint":
                    DiscordClient.Rest.SendMessageAsync(DiscordChannelID,
                        "### !RenameCheckpoint \n" +
                        " - I'll rename a checkpoint for you to something else.\n" +
                        " - Usage: !RenameCheckpoint [activity shorthand(!activities)] [(optional)master] [single word name given to the checkpoint] [new name for given checkpoint. a-z, 0-9 only]");
                    done = true;
                    return;
            }
            if (!done)
            {
                DiscordClient.Rest.SendMessageAsync(DiscordChannelID,
                    "The command you asked about doesn't seem to exist.\nPlease verify your spelling and try again or use the !ListCommands command.");
            }
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }
    }

    public class CommandList : IChoicesProvider<SlashCommandContext>
    {
        public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(SlashCommandParameter<SlashCommandContext> parameter)
        {
            IEnumerable<ApplicationCommandOptionChoiceProperties> choices = [
                new ApplicationCommandOptionChoiceProperties("Help", "Help"),
                new ApplicationCommandOptionChoiceProperties("ListCommands", "ListCommands"),
                new ApplicationCommandOptionChoiceProperties("Activities", "Activities"),
                new ApplicationCommandOptionChoiceProperties("HoldLoad", "HoldLoad"),
                new ApplicationCommandOptionChoiceProperties("EndHold", "EndHold"),
                new ApplicationCommandOptionChoiceProperties("GrabCheckpoint", "GrabCheckpoint"),
                new ApplicationCommandOptionChoiceProperties("ForceWipe", "ForceWipe"),
                new ApplicationCommandOptionChoiceProperties("LaunchAndHold", "LaunchAndHold"),
                new ApplicationCommandOptionChoiceProperties("DeleteCheckpoint", "DeleteCheckpoint"),
                new ApplicationCommandOptionChoiceProperties("ListCheckpoints", "ListCheckpoints"),
                new ApplicationCommandOptionChoiceProperties("ListFeats", "ListFeats"),
                new ApplicationCommandOptionChoiceProperties("FarmCheckpoint", "FarmCheckpoint"),
                new ApplicationCommandOptionChoiceProperties("EndFarm", "EndFarm"),
                new ApplicationCommandOptionChoiceProperties("TransferCheckpoint", "TransferCheckpoint"),
                new ApplicationCommandOptionChoiceProperties("FlyInCheckpointTransfer", "FlyInCheckpointTransfer"),
                new ApplicationCommandOptionChoiceProperties("GrabCheckpointAndConfirm", "GrabCheckpointAndConfirm"),
                new ApplicationCommandOptionChoiceProperties("RenameCheckpoint", "RenameCheckpoint"),
                new ApplicationCommandOptionChoiceProperties("CleanCheckpoints", "CleanCheckpoints"),
                new ApplicationCommandOptionChoiceProperties("ForceRestart", "ForceRestart"),
            ];

            return ValueTask.FromResult(choices);
        }
    }

    public class ActivityProvider : IChoicesProvider<SlashCommandContext>
    {
        public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(SlashCommandParameter<SlashCommandContext> parameter)
        {
            IEnumerable<ApplicationCommandOptionChoiceProperties> choices = [
                new ApplicationCommandOptionChoiceProperties("Crota's End", "CE"),
                new ApplicationCommandOptionChoiceProperties("Deepstone Crypt", "DSC"),
                new ApplicationCommandOptionChoiceProperties("The Desert Perpetual", "DP"),
                new ApplicationCommandOptionChoiceProperties("The Desert Perpetual (Epic)", "DPE"),
                new ApplicationCommandOptionChoiceProperties("Salvation's Edge", "SE"),
                new ApplicationCommandOptionChoiceProperties("Root of Nightmares", "RON"),
                new ApplicationCommandOptionChoiceProperties("Kingsfall", "KF"),
                new ApplicationCommandOptionChoiceProperties("Vow of the Disciple", "VOW"),
                new ApplicationCommandOptionChoiceProperties("Vault of Glass", "VOG"),
                new ApplicationCommandOptionChoiceProperties("Garden of Salvation", "GOS"),
                new ApplicationCommandOptionChoiceProperties("Last Wish", "LW"),
                new ApplicationCommandOptionChoiceProperties("Warlord's Ruin", "WR"),
                new ApplicationCommandOptionChoiceProperties("Pit of Herasy", "PIT"),
                new ApplicationCommandOptionChoiceProperties("Equilibrium", "EQ"),
                new ApplicationCommandOptionChoiceProperties("Sundered Doctrine", "SD"),
                new ApplicationCommandOptionChoiceProperties("Vesper's Host", "VH"),
                new ApplicationCommandOptionChoiceProperties("Ghosts of the Deep", "GOTD"),
                new ApplicationCommandOptionChoiceProperties("Spire of the Watcher", "SOTW"),
                new ApplicationCommandOptionChoiceProperties("Duality", "D"),
                new ApplicationCommandOptionChoiceProperties("Grasp of Avarice", "GOA"),
                new ApplicationCommandOptionChoiceProperties("Prophecy", "PR"),
                new ApplicationCommandOptionChoiceProperties("The Shattered Throne", "ST"),
                new ApplicationCommandOptionChoiceProperties("Pantheon Calus Resplendant", "CR"),
                new ApplicationCommandOptionChoiceProperties("Pantheon Morgeth Surpassing", "MS"),
                new ApplicationCommandOptionChoiceProperties("Pantheon Insurrection Prime", "GAUNTLET")
            ];

            return ValueTask.FromResult(choices);
        }
    }

    public class IsMasterAutoComplete : IAutocompleteProvider<AutocompleteInteractionContext>
    {
        public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(ApplicationCommandInteractionDataOption option, AutocompleteInteractionContext context)
        {
            List<ApplicationCommandOptionChoiceProperties> retcheck = new();

            string activity = "";
            foreach (ApplicationCommandInteractionDataOption temp in context.Interaction.Data.Options)
            {
                if (temp.Name == "activity") activity = temp.Value;
            }

            if (activity != null)
            {
                if (Checkpoints[activity].Count > 0) retcheck.Add(new ApplicationCommandOptionChoiceProperties("False", "false"));

                activity = "master" + activity;
                if (Checkpoints.Keys.Contains(activity))
                {
                    if (Checkpoints[activity].Count > 0) retcheck.Add(new ApplicationCommandOptionChoiceProperties("True", "true"));
                }
            }

            return ValueTask.FromResult(retcheck.AsEnumerable());
        }
    }

    public class IsMasterOrFeatsAutoComplete : IAutocompleteProvider<AutocompleteInteractionContext>
    {
        public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(ApplicationCommandInteractionDataOption option, AutocompleteInteractionContext context)
        {
            List<ApplicationCommandOptionChoiceProperties> retcheck = new();

            string activity = "";
            foreach (ApplicationCommandInteractionDataOption temp in context.Interaction.Data.Options)
            {
                if (temp.Name == "activity") activity = temp.Value;
            }

            if (activity != null)
            {
                if (Checkpoints.Keys.Contains("master" + activity))
                {
                    if (Checkpoints[activity].Count > 0) retcheck.Add(new ApplicationCommandOptionChoiceProperties("False", "false"));
                    if (Checkpoints["master" + activity].Count > 0) retcheck.Add(new ApplicationCommandOptionChoiceProperties("True", "true"));
                }
                else
                {
                    if (Checkpoints[activity].Count > 0) retcheck.Add(new ApplicationCommandOptionChoiceProperties("False", "false"));
                    if(activity == "DP" || activity == "EDP" || activity == "EQ")
                    {
                        string[] possiblefeats = { "token", "phase", "battalions", "challenges", "cutthroat" };
                        string currentstring = option.Value;
                        if (currentstring == null) currentstring = "";
                        if (!currentstring.Contains("feats:")) retcheck.Add(new ApplicationCommandOptionChoiceProperties("feats:", "feats:"));
                        else
                        {
                            string[] activefeats = currentstring.Replace("feats:", "").Split(",");
                            if (activefeats.Last() != "") currentstring = currentstring.Replace(activefeats.Last(), "");
                            if (!currentstring.Contains("token,")) retcheck.Add(new ApplicationCommandOptionChoiceProperties(currentstring + "token,", currentstring + "token,"));
                            if (!currentstring.Contains("phase,")) retcheck.Add(new ApplicationCommandOptionChoiceProperties(currentstring + "phase,", currentstring + "phase,"));
                            if (!currentstring.Contains("battalions,")) retcheck.Add(new ApplicationCommandOptionChoiceProperties(currentstring + "battalions,", currentstring + "battalions,"));
                            if (!currentstring.Contains("challenges,")) retcheck.Add(new ApplicationCommandOptionChoiceProperties(currentstring + "challenges,", currentstring + "challenges,"));
                            if (!currentstring.Contains("cutthroat,")) retcheck.Add(new ApplicationCommandOptionChoiceProperties(currentstring + "cutthroat,", currentstring + "cutthroat,"));
                        }
                    }
                }
            }

            return ValueTask.FromResult(retcheck.AsEnumerable());
        }
    }

    public class CanMaster : IAutocompleteProvider<AutocompleteInteractionContext>
    {
        public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(ApplicationCommandInteractionDataOption option, AutocompleteInteractionContext context)
        {
            List<ApplicationCommandOptionChoiceProperties> retcheck = new();

            string activity = "";
            foreach (ApplicationCommandInteractionDataOption temp in context.Interaction.Data.Options)
            {
                if (temp.Name == "activity") activity = temp.Value;
            }

            retcheck.Add(new ApplicationCommandOptionChoiceProperties("False", "false"));

            activity = "master" + activity;
            if (Checkpoints.Keys.Contains(activity))
            {
                retcheck.Add(new ApplicationCommandOptionChoiceProperties("True", "true"));
            }

            return ValueTask.FromResult(retcheck.AsEnumerable());
        }
    }

    public class CheckpointAutocomplete : IAutocompleteProvider<AutocompleteInteractionContext>
    {
        public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(ApplicationCommandInteractionDataOption option, AutocompleteInteractionContext context)
        {
            List<ApplicationCommandOptionChoiceProperties> retcheck = new();
            string activity = "";
            foreach(ApplicationCommandInteractionDataOption temp in context.Interaction.Data.Options)
            {
                if (temp.Name == "activity") activity = temp.Value;
            }

            string master = "";

            foreach (ApplicationCommandInteractionDataOption temp in context.Interaction.Data.Options)
            {
                if (temp.Name == "master") master = temp.Value;
            }

            bool ismaster = (master.ToLower() == "true");
            if (ismaster)
            {
                activity = "master" + activity;
            }

            string[] str = Checkpoints[activity].Keys.ToArray();
            for (int i = 0; i < str.Length; i++)
            {
                retcheck.Add(new ApplicationCommandOptionChoiceProperties(str[i], str[i].ToLower()));
            }

            return ValueTask.FromResult(retcheck.AsEnumerable());
        }
    }

    public class ActivitiesWithCheckpoints : IAutocompleteProvider<AutocompleteInteractionContext>
    {
        public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(ApplicationCommandInteractionDataOption option, AutocompleteInteractionContext context)
        {
            List<ApplicationCommandOptionChoiceProperties> retcheck = new();

            foreach(string activ in Checkpoints.Keys)
            {
                string[] str = Checkpoints[activ].Keys.ToArray();
                if(str.Length > 0)
                {
                    if (activ.Contains("master"))
                    {
                        string[] str2 = Checkpoints[activ.Replace("master", "")].Keys.ToArray();
                        if (str2.Length == 0) retcheck.Add(new ApplicationCommandOptionChoiceProperties(ConvertActivShorthandToFullName(activ.Replace("master", "")), activ.Replace("master", "")));
                    }
                    else
                    {
                        retcheck.Add(new ApplicationCommandOptionChoiceProperties(ConvertActivShorthandToFullName(activ), activ));
                    }
                }
            }

            return ValueTask.FromResult(retcheck.AsEnumerable());
        }
    }
}
