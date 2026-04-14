namespace UrlRedirect.Web.Security;

public sealed class AdminAuthorizationOptions
{
    public const string SectionName = "Authorization:Admin";

    public string[] AllowedGroupIds { get; set; } = [];

    public string[] AllowedRoles { get; set; } = [];
}
