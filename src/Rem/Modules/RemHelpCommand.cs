﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Rem.Common.Preconditions;
using Rem.Common;
using System.Text;

namespace Rem.Modules
{
    [Name("Help"), Group]
    public sealed class RemHelpCommand : ModuleBase
    {
        private readonly CommandService RemService;
        private readonly IDependencyMap RemDeps;

        public RemHelpCommand(CommandService _RemService, IDependencyMap _RemDeps)
        {
            if (_RemService == null) throw new ArgumentNullException(nameof(_RemService));

            RemService = _RemService;
            RemDeps = _RemDeps;
        }

        [Command("Help"), Summary("View all my commands!"), Hidden]
        public async Task CommandHelp()
        {
            IEnumerable<IGrouping<string, CommandInfo>> CommandGroups = (await RemService.Commands.CheckConditions(Context, RemDeps))
                .Where(c => !c.Preconditions.Any(p => p is HiddenAttribute))
                .GroupBy(c => (c.Module.IsSubmodule ? c.Module.Parent.Name : c.Module.Name));

            NormalEmbed HelpEmbed = new NormalEmbed();
            StringBuilder HEDesc = new StringBuilder();

            HelpEmbed.Title = "My Commands";
            HelpEmbed.ThumbnailUrl = Context.Client.CurrentUser.AvatarUrl;
            HEDesc.AppendLine("**You can use the following commands:**");

            foreach (IGrouping<string, CommandInfo> Group in CommandGroups)
            {
                if (Group.Key == "Reactions" || Group.Key == "Emojis")
                {
                    StringBuilder sbx = new StringBuilder();
                    List<string> reactions = new List<string> { };
                    foreach (CommandInfo Command in Group)
                    {
                        reactions.Add("`" + Command.Name + "` ");
                        sbx.Append($"`{Command.Name}` ");

                    }
                    HEDesc.AppendLine($"**{Group.Key}**: " + sbx.ToString());
                }
                else
                {
                    HEDesc.AppendLine($"**{Group.Key}**:");
                    foreach (CommandInfo Command in Group)
                    {
                        HEDesc.AppendLine($"• `{Command.Name}`: {Command.Summary}");
                    }
                }
            }
            HEDesc.AppendLine($"\nYou can use `{Rem.RemConfig["Command_Prefix"]}Help <command>` for more information on that command");

            HelpEmbed.Description = HEDesc.ToString();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("**See my source code!** https://github.com/iloverem/Rem");
            await (Context.User.CreateDMChannelAsync().Result).SendMessageAsync(sb.ToString(), false, HelpEmbed);
            if (!Context.IsPrivate)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention}, help sent to your Direct Messages!");
            }
        }

        [Command("Help"), Summary("Shows summary for a command or group."), Hidden]
        public async Task SpecificHelp(string cmdname)
        {
            StringBuilder sb = new StringBuilder();
            NormalEmbed e = new NormalEmbed();
            IEnumerable<CommandInfo> Commands = (await RemService.Commands.CheckConditions(Context, RemDeps))
                .Where(c => (c.Aliases.FirstOrDefault().Equals(cmdname, StringComparison.OrdinalIgnoreCase) ||
                (c.Module.IsSubmodule ? c.Module.Aliases.FirstOrDefault().Equals(cmdname, StringComparison.OrdinalIgnoreCase) : false))
                    && !c.Preconditions.Any(p => p is HiddenAttribute));

            if (Commands.Any())
            {
                await ReplyAsync($"{Commands.Count()} {(Commands.Count() > 1 ? $"entries" : "entry")} for `{cmdname}`");

                foreach (CommandInfo Command in Commands)
                {
                    NormalEmbed x = new NormalEmbed();
                    x.Title = $"{Command.Name}";
                    x.Description = (Command.Summary ?? "No summary.");
                    x.AddField(a =>
                    {
                        a.Name = "Usage";
                        a.IsInline = true;
                        a.Value = $"{Rem.RemConfig["Command_Prefix"]}{(Command.Module.IsSubmodule ? $"{Command.Module.Name} " : "")}{Command.Name} " + string.Join(" ", Command.Parameters.Select(p => formatParam(p))).Replace("`", "") + " ";
                    });
                    x.AddField(a =>
                    {
                        a.Name = "Aliases";
                        a.IsInline = true;
                        StringBuilder s = new StringBuilder();
                        if (Command.Aliases.Any())
                        {
                            foreach (string Alias in Command.Aliases)
                            {
                                s.Append(Alias + " ");
                            }
                        }
                        a.Value = $"{(Command.Aliases.Any() ? s.ToString() : "No aliases.")}";
                    });
                    await Context.Channel.SendEmbedAsync(x);
                }
            }
            else
            {
                await ReplyAsync($":warning: I couldn't find any command matching `{cmdname}`.");
                return;
            }
        }

        private string formatParam(ParameterInfo param)
        {
            var sb = new StringBuilder();
            if (param.IsMultiple)
            {
                sb.Append($"`[({param.Type.Name}): {param.Name}...]`");
            }
            else if (param.IsRemainder)
            {
                sb.Append($"`<({param.Type.Name}): {param.Name}...>`");
            }
            else if (param.IsOptional)
            {
                sb.Append($"`[({param.Type.Name}): {param.Name}]`");
            }
            else
            {
                sb.Append($"`<({param.Type.Name}): {param.Name}>`");
            }

            if (!string.IsNullOrWhiteSpace(param.Summary))
            {
                sb.Append($" ({param.Summary})");
            }
            return sb.ToString();
        }
    }
}
