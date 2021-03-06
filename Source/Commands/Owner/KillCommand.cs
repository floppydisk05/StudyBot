using System;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

using StudyBot.Commands.Attributes;

using StudyBot.Misc;

namespace StudyBot.Commands.Owner
{
    public class KillCommand : BaseCommandModule
    {
        [Command("kill")]
        [Description("Kills the bot")]
        [Category(Category.Owner)]
        public async Task Kill(CommandContext Context)
        {
            if(Context.User.Id != Bot.config.ownerId && !Bot.whitelistedUsers.Contains(Context.User.Id))
				throw new Exception("You must be the bot owner to run this command!");
			
			await Context.ReplyAsync("Shutting down...");
			await Bot.logChannel.SendMessageAsync("Shutdown triggered by command");
			DailyReportSystem.CreateBackup();
            UserData.SaveData();
			Environment.Exit(0);
        }
    }
}