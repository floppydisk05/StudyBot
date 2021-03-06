using System;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

using StudyBot.Commands.Attributes;

using StudyBot.Misc;

namespace StudyBot.Commands.Owner
{
    public class WhitelistCommand : BaseCommandModule
    {
        [Command("whitelist")]
        [Description("Whitelist a user to use owner commands temporarily")]
        [Usage("[user]")]
        [Category(Category.Owner)]
        public async Task Whitelist(CommandContext Context, DiscordUser user)
        {
            if(Context.User.Id != Bot.config.ownerId && !Bot.whitelistedUsers.Contains(Context.User.Id))
				throw new Exception("You must be the bot owner to run this command!");
			
			Bot.whitelistedUsers.Add(user.Id);
            await Context.ReplyAsync("Whitelisted " + user.Mention);
        }
    }
}