# StudyBot
StudyBot is a Discord bot for... uhh... something.

## Usage/Build instructions
1. Clone the repository: ``https://github.com/floppydisk05/StudyBot.git`` and move into the project directory: ``cd StudyBot``
2. Build the source code: ``dotnet build -c Release -r linux-x64``
3. Change into the build directory: ``cd bin/Release/net5.0/linux-x64/``
4. Run the bot: ``./StudyBot`` or just ``StudyBot`` for Windows. This will generate a blank configuration file for you.
5. Edit the ``config.json``  file and add your token into the token field, aswell as your client ID and log channel if you want one
6. Run the bot once more, as before. Once it has started up (It'll output "Ready" to the terminal), you should be good to go into Discord and use it.

*If you're on Windows there's some changes you need to make to the build process but I'm sure you can figure that out, as you do.*
