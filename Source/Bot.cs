using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Reflection;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Collections.Generic;

using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

using Newtonsoft.Json;

using Serilog;

using Microsoft.Extensions.Logging;

using StudyBot.Util;
using StudyBot.Misc;

namespace StudyBot
{
    class Bot
    {
        public const string VERSION = "3.6.3";

        static void Main(string[] args) => new Bot().RunBot().GetAwaiter().GetResult();

        public static DiscordClient client;
        public static CommandsNextExtension commands;
        public static BotConfig config;
        public static DiscordChannel logChannel = null;
        public static List<ulong> blacklistedUsers = new List<ulong>();
        public static List<ulong> whitelistedUsers = new List<ulong>();


        public async Task RunBot()
        {
            // Change the working directory for debug mode
#if DEBUG
            if (!Directory.Exists("WorkingDirectory"))
                Directory.CreateDirectory("WorkingDirectory");
            Directory.SetCurrentDirectory("WorkingDirectory");
#endif

            // Logger
            Log.Logger = new LoggerConfiguration()
                .WriteTo.DiscordSink()
                .CreateLogger();
            var logFactory = new LoggerFactory().AddSerilog();

            // Verify directory structure
            if (!Directory.Exists("Logs"))
                Directory.CreateDirectory("Logs");
            if(!Directory.Exists("Cache"))
                Directory.CreateDirectory("Cache");

            // Load blacklisted users
            if(!File.Exists("blacklist.json"))
                File.WriteAllText("blacklist.json", "[]");
            blacklistedUsers = JsonConvert.DeserializeObject<List<ulong>>(File.ReadAllText("blacklist.json"));

            // Load the config
            if (File.Exists("config.json"))
                config = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText("config.json"));
            else
            {
                // Create a blank config
                config = new BotConfig();
                config.token = "TOKEN";
                config.status = " ";
                config.prefix = ".";

                // Write the config and quit
                File.WriteAllText("config.json", JsonConvert.SerializeObject(config, Formatting.Indented));
                Console.WriteLine("No configuration file found. A template config has been written to config.json");
                return;
            }

            // Set up the client
            client = new DiscordClient(new DiscordConfiguration()
            {
                Token = config.token,
                TokenType = TokenType.Bot,
                LoggerFactory = logFactory,
                Intents = DiscordIntents.All
            });
            commands = client.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] { config.prefix },
                EnableDefaultHelp = false,
                EnableDms = true,
                UseDefaultCommandHandler = false
            });

            // Events
            commands.CommandErrored += async (CommandsNextExtension cnext, CommandErrorEventArgs e) =>
            {
                string msg = e.Exception.Message;
                if(msg == "One or more pre-execution checks failed.")
                    msg += " This is likely a permissions issue.";
                
                await logChannel.SendMessageAsync($"**Command Execution Failed!**\n**Command:** `{e.Command.Name}`\n**Message:** `{e.Context.Message.Content}`\n**Exception:** `{e.Exception}`");
                await e.Context.ReplyAsync($"There was an error executing your command!\nMessage: `{msg}`");
                
                // string usage = StudyBot.Commands.Main.HelpCommand.GetCommandUsage(e.Command.Name);
                // if (usage != null)
                // {
                //     string upperCommandName = e.Command.Name[0].ToString().ToUpper() + e.Command.Name.Remove(0, 1);
                //     DiscordEmbedBuilder eb = new DiscordEmbedBuilder();
                //     eb.WithColor(DiscordColor.Aquamarine);
                //     eb.WithTitle($"{upperCommandName} Command");
                //     eb.WithDescription($"{usage}");
                //     await e.Context.RespondAsync("Are you sure you used it correctly?", eb.Build());
                // }
            };
            client.Ready += async (DiscordClient client, ReadyEventArgs e) => {
                
                logChannel = await client.GetChannelAsync(config.logChannel);
                if(logChannel == null) {
                    throw new Exception("Shitcord is failing to return a log channel");
                }

                UserData.Init();
                DailyReportSystem.Init();
                Cache.InitTimer();
                
                await client.UpdateStatusAsync(new DiscordActivity() { Name = config.status });
                Log.Write(Serilog.Events.LogEventLevel.Information, $"Running on host: {MiscUtil.GetHost().Replace('\n', ' ')}");
                Log.Write(Serilog.Events.LogEventLevel.Information, "Ready");
            };
            // Edit logging
            client.MessageUpdated += async (DiscordClient client, MessageUpdateEventArgs e) => {
                if(e.MessageBefore.Content == e.Message.Content) // Just fixing Discords issues.... ffs
                    return;

                DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
                builder.WithColor(DiscordColor.Aquamarine);
                builder.WithDescription($"**{e.Author.Username}#{e.Author.Discriminator}** updated a message in {e.Channel.Mention} \n" + Formatter.MaskedUrl("Jump to message!", e.Message.JumpLink));
                builder.AddField("Before", e.MessageBefore.Content, true);
                builder.AddField("After", e.Message.Content, true);
                builder.AddField("IDs", $"```cs\nUser = {e.Author.Id}\nMessage = {e.Message.Id}\nChannel = {e.Channel.Id}```");
                builder.WithTimestamp(DateTime.Now);
                await logChannel.SendMessageAsync("", builder.Build());
            };
            // Delete logging
            client.MessageDeleted += async (DiscordClient client, MessageDeleteEventArgs e) => {
                DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
                builder.WithColor(DiscordColor.Aquamarine);
                builder.WithDescription($"**{e.Message.Author.Username}#{e.Message.Author.Discriminator}**'s message in {e.Channel.Mention} was deleted");
                builder.AddField("Content", e.Message.Content, true);
                builder.AddField("IDs", $"```cs\nUser = {e.Message.Author.Id}\nMessage = {e.Message.Id}\nChannel = {e.Channel.Id}```");
                builder.WithTimestamp(DateTime.Now);
                await logChannel.SendMessageAsync("", builder.Build());
            };
            client.MessageCreated += CommandHandler;
            
            // Commands
            commands.RegisterCommands(Assembly.GetExecutingAssembly());

            // Connect
            await client.ConnectAsync();

            await Task.Delay(-1);
        }

        private async Task CommandHandler(DiscordClient client, MessageCreateEventArgs e)
        {
            DiscordMessage msg = e.Message;
            
            if(blacklistedUsers.Contains(msg.Author.Id) || e.Author.IsBot)
                return;
                
            // Prefix check
            int start = msg.GetStringPrefixLength(config.prefix);
            if(start == -1) return;

            string prefix = msg.Content.Substring(0, start);
            string cmdString = msg.Content.Substring(start);

            // Multi-command check and execution
            if(cmdString.Contains(" && ")) {
                string[] commands = cmdString.Split(" && ");
                if(commands.Length > 2 && e.Author.Id != client.CurrentApplication.Owners.FirstOrDefault().Id) return;
                for(int i = 0; i < commands.Length; i++) {
                    DoCommand(commands[i], prefix, msg);
                }
                return;
            }

            // Execute single command
            Command cmd = commands.FindCommand(cmdString, out var args);
            if(cmd == null) return;
            CommandContext ctx = commands.CreateContext(msg, prefix, cmd, args);
            await Task.Run(async () => await commands.ExecuteCommandAsync(ctx).ConfigureAwait(false));
        }

        private void DoCommand(string commandString, string prefix, DiscordMessage msg) {
            Command cmd = commands.FindCommand(commandString, out var args);
            if(cmd == null) return;
            CommandContext ctx = commands.CreateFakeContext(msg.Author, msg.Channel, commandString, prefix, cmd, args);
            _ = Task.Run(async () => await commands.ExecuteCommandAsync(ctx).ConfigureAwait(false));
        }
    }

    class BotConfig
    {
        public string token { get; set; }
        public string prefix { get; set; } = ".";
        public string status { get; set; }
        public ulong logChannel { get; set; }
        public ulong ownerId { get; set; }
    }
}
