using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace UrlRedirect.Web.Security;

public sealed class AdminAccessHandler : AuthorizationHandler<AdminAccessRequirement>
{
    private readonly IOptions<AdminAuthorizationOptions> _options;

    public AdminAccessHandler(IOptions<AdminAuthorizationOptions> options)
    {
        _options = options;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminAccessRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var configuredRoles = _options.Value.AllowedRoles
            .Where(static role => !string.IsNullOrWhiteSpace(role))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var configuredGroups = _options.Value.AllowedGroupIds
            .Where(static group => !string.IsNullOrWhiteSpace(group))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (configuredRoles.Count == 0 && configuredGroups.Count == 0)
        {
            return Task.CompletedTask;
        }

        var userRoles = context.User.FindAll(ClaimTypes.Role)
            .Concat(context.User.FindAll("roles"))
            .Select(static claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var userGroups = context.User.FindAll("groups")
            .Select(static claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (userRoles.Overlaps(configuredRoles) || userGroups.Overlaps(configuredGroups))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
