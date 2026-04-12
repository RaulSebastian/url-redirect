using UrlRedirect.Domain.Validation;
using Xunit;

namespace UrlRedirect.Web.Tests;

public sealed class RedirectRequestValidatorTests
{
    [Theory]
    [InlineData("ui")]
    [InlineData("api")]
    public void Validate_RejectsReservedAliases(string alias)
    {
        var errors = RedirectRequestValidator.Validate(alias, "https://example.com");

        Assert.True(errors.ContainsKey("Alias"));
        Assert.Contains(errors["Alias"], message => message.Contains("reserved", StringComparison.OrdinalIgnoreCase));
    }
}
