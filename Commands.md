# Getting checkpoints from the bot:


# Giving checkpoints to the bot:
### GrabCheckpoint:
The bot will join on the username given with the command and idle waiting for a wipe. When the bot detects a wipe screen it will then save the checkpoint and return to orbit.\
Usage: !GrabCheckpoint [activity shorthand (!activities)] [(optional)master/m] [single word name for the checkpoint of your choosing.  a-z, 1-9 only] BungieUsername#0000

### GrabCheckpointAndConfirm:
Same as the above command, however instead of verifying it's got the checkpoint via wipe screen it instead waits for the user to run the follow-up command "!endhold". At which point the bot will return to orbit and verify that it has the checkpoint before returning to character select and idling.\
Usage: !GrabCheckpointAndConfirm [activity shorthand (!activities)] [(optional)master/m] [single word name for the checkpoint of your choosing.  a-z, 1-9 only] BungieUsername#0000

### FlyInCheckpointTransfer:
Uses a quark with flying into an activity and abandoning the lobby to transfer a checkpoint to the bot. Useful for transferring non-darkness zone checkpoints to the bot, though it doesn't always work.\
The bot will join on the username given with the command and send a message in chat telling you when to launch. When you launch the activity you'll then be expected to navigate to your settings and find the "change character" button.\
After this is done, the bot will then send a chat message telling you to change characters. Shortly after this the bot will return back to orbit and verify if it does, or doesn't have the checkpoint.\
Usage: !flyincheckpointtransfer [activity shorthand (!activities)] [(optional)master/m] [single word name of the checkpoint.  a-z, 1-9 only] BungieUsername#0000

# Administrative commands:
### Help:
!Help [command] to see a detailed description of any other command. Detailed descriptions also listed below.

### ListCommands:
!ListCommands will list out every command here, and show a 1-2 sentence description of it.

### Activities:
!Activities will show the full list of shorthands used for every activity the bot recognizes. 

### ListCheckpoints:
!ListCheckpoints prints out a list of all checkpoints currently held by the bot.

### RenameCheckpoint:
Will rename any checkpoint to have a different name so long as it is unique.\
usage: !RenameCheckpoint [activity shorthand] [(optional)master/m] [current name of checkpoint] [new desired name]

### CleanCheckpoints:
!CleanCheckpoints will first verify, and then spend the next 30 minutes or so going thru and cleaning out any vestigial checkpoints that the bot doesn't have saved in it's database.

### ForceRestart:
!Forcerestart will first check to verify you want to do it, and then will kill the d2 process thus tricking the bot into thinking the game crashed and will instantly restart the program. Good for getting out of stuck spots for the bot.
