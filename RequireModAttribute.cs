using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace QuaverBot;

public class RequireModAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
    {
        IUser user = context.User;
        IGuildUser guildUser = user as IGuildUser ?? context.Guild.GetUserAsync(user.Id).Result;
        if (guildUser == null)
        {
            return Task.FromResult(PreconditionResult.FromError("Command must be used in a guild channel."));
        }

        if (!guildUser.RoleIds.Any(r => QuaverBot.Config.ModRoles.Contains(r)))
        {
            return Task.FromResult(PreconditionResult.FromError("You don't have permission to use this command."));
        }

        return Task.FromResult(PreconditionResult.FromSuccess());
    }
}