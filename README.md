
# D2 Single Server Checkpoint Bot

This is a bot initially designed specifically for my clans use. It requires a sacrificial windows PC, a sacrificial d2 account that has access to all activities, and a willingness to register a new bot with Discord.

## Requirements to run:

1. [.NET 10 sdk](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) for runtimes
2. [Vigembus](https://github.com/nefarius/ViGEmBus/releases) to emulate an xbox controller. This will need to be installed twice for some reason. If it's not installed the bot will get stuck in a loop.
3. An unused computer running Windows 10 or higher.
4. Your own [Discord Bot](https://discord.com/developers/) registered, complete with "Mesage Content" intent, currently added to the server you wish to have the bot in.
5. A discord server you have admin permissions in.
6. A sacrificial Destiny 2 account to be used as a bot, running specifically on Steam.

## Required/Recommended settings:
1. 30FPS cap (recommended)
2. Resolution NOT stretched or windowed. Fullscreen at whatever resolution is native, or the display is set to. 1920x1080 or larger. (Required)
3. HDR off & Brightness 6 (Recommended but shouldnt be required)
note, this was designed on/works well on ultrawide. Resolutions below 1920x1080 may have issues with character recognition as I've not tested anything below that. 

## Instructions

1. Make sure you've gone thru the ^above requirements and gotten everything together, as well as installed this program from the releases page.
2. Extract the program from it's zip file into its own folder.
3. Ensure Steam is open and running, on specifically the account you want the bot to run on.
4. Go into the Steam friends list page, and change your online visiblility to "Invisible", OR ensure Destiny 2 is set to invite only.
5. Ensure Destiny's brightness setting is somewhere above 5. It should work on any resolution and at any brightness, but I tested it specifically on default settings for everything.
6. Launch TnTCheckpoint.exe, and follow its instructions. It will ask you for a channel ID to interact in, as well as the bot's access token. Once it opens Destiny 2, release your keyboard and mouse, and give no more input. All required interaction past this point should be entirely handled thru the Discord bot from another device. 

## This was made ENTIRELY without the use of GenAI, or LLM's. And despite being entirely open source, Licensed under MIT, it is my wishes that this code isn't used for any sort of training data, or anything used for LLM's or Generative AI. Thank you.
