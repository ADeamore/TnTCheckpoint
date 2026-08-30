# Getting checkpoints from the bot
### TransferCheckpoint:
The bot will wait in orbit on the activity, and invite the username given with the command. Once the person joins the bot will then launch, and once loading has gotten far enough to give the checkpoint to the other person the bot will leave.\
Usage: !TransferCheckpoint `[activity shorthand(!activities)] [(optional)master] [single word name given to the checkpoint.] BungieUsername#0000"`
### FarmCheckpoint:
The bot will perform the above command, but on a loop so that after every time you load into the activity it will then invite you to relaunch again. Useful for solo or low-man farming checkpoints. This command will also work with feats unlike the above command and will run until the !endfarm command is run.\
usage: `!FarmCheckpoint [activity shorthand (!activities)] [(optional)master] [(optional)feats:feat1name,feat2name,etc...] [single word name for the checkpoint.]  BungieUsername#0000`\
example: `!FarmCheckpoint EQ feats:tokenlimit,phaselimit shockyhands GuardianName#1234`

# Giving checkpoints to the bot
### GrabCheckpoint:
The bot will join on the username given with the command and idle waiting for a wipe. When the bot detects a wipe screen it will then save the checkpoint and return to orbit.\
Usage: `!GrabCheckpoint [activity shorthand (!activities)] [(optional)master/m] [single word name for the checkpoint.] BungieUsername#0000`

### GrabCheckpointAndConfirm:
Same as the above command, however instead of verifying it's got the checkpoint via wipe screen it instead waits for the user to run the follow-up command "!endhold". At which point the bot will return to orbit and verify that it has the checkpoint before returning to character select and idling.\
Usage: `!GrabCheckpointAndConfirm [activity shorthand (!activities)] [(optional)master/m] [single word name for the checkpoint.] BungieUsername#0000`

### FlyInCheckpointTransfer:
Uses a quark with flying into an activity and abandoning the lobby to transfer a checkpoint to the bot. Useful for transferring non-darkness zone checkpoints to the bot, though it doesn't always work.\
The bot will join on the username given with the command and send a message in chat telling you when to launch. When you launch the activity you'll then be expected to navigate to your settings and find the "change character" button.\
After this is done, the bot will then send a chat message telling you to change characters. Shortly after this the bot will return back to orbit and verify if it does, or doesn't have the checkpoint.\
Usage: `!flyincheckpointtransfer [activity shorthand (!activities)] [(optional)master/m] [single word name of the checkpoint.] BungieUsername#0000`

### ForceWipe:
Useful for the GrabCheckpoint command. The bot will look down, swap to it's heavy, and pull the trigger. Only useful if the bot has a rocket in it's 3rd slot.

# Useful bot tools
### HoldLoad:
Has the bot join on the given player and holds the load, occasionally sending the player an invite so that they may rejoin on other characters or just join back later.\
usage: `!HoldLoad BungieUsername#0000`
### LaunchAndHold:
Has the bot launch a given activity, and then hold the load until the "!EndHold" command is run while periodically sending the user invites. Effectively a combination of the "!HoldLoad" command and the "!TransferCheckpoint" command.\
Usage: `!LaunchAndHold [activity shorthand(!activities)] [(optional)master/m] [single word name given to the checkpoint.] BungieUsername#0000`

# Administrative commands
### Help:
!Help [command] to see a detailed description of any other command. Detailed descriptions also listed below.

### ListCommands:
!ListCommands will list out every command here, and show a 1-2 sentence description of it.

### Activities:
!Activities will show the full list of shorthands used for every activity the bot recognizes. 

### ListCheckpoints:
!ListCheckpoints prints out a list of all checkpoints currently held by the bot.

### ListFeats:
!ListFeats lists out all feats as the bot expects them to be written.\
`Token, Phase, Battalions, Challenges, and Cutthroat.`

### DeleteCheckpoint:
Deletes a checkpoint from memory.\
usage: `!DeleteCheckpoint [activity shorthand (!activities)] [(optional)master/m] [single word name of the checkpoint.]`

### RenameCheckpoint:
Will rename any checkpoint to have a different name so long as it is unique.\
usage: `!RenameCheckpoint [activity shorthand] [(optional)master/m] [current name of checkpoint] [new desired name]`

### CleanCheckpoints:
!CleanCheckpoints will first verify, and then spend the next 30 minutes or so going thru and cleaning out any vestigial checkpoints that the bot doesn't have saved in it's database.

### ForceRestart:
!Forcerestart will first check to verify you want to do it, and then will kill the d2 process thus tricking the bot into thinking the game crashed and will instantly restart the program. Good for getting out of stuck spots for the bot.
