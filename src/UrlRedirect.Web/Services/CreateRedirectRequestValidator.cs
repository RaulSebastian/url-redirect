using System.ComponentModel.DataAnnotations;
using UrlRedirect.Web.Models;

namespace UrlRedirect.Web.Services;

public static class CreateRedirectRequestValidator
{
    public static IReadOnlyDictionary<string, string[]> Validate(CreateRedirectInput input)
    {
        input.Alias = input.Alias.Trim().ToLowerInvariant();
        input.TargetUrl = input.TargetUrl.Trim();

        var validationContext = new ValidationContext(input);
        var validationResults = new List<ValidationResult>();

        Validator.TryValidateObject(input, validationContext, validationResults, validateAllProperties: true);

        if (!Uri.TryCreate(input.TargetUrl, UriKind.Absolute, out _))
        {
            validationResults.Add(new ValidationResult(
                "Enter a valid absolute URL.",
                [nameof(CreateRedirectInput.TargetUrl)]));
        }

        return validationResults
            .SelectMany(result =>
            {
                var members = result.MemberNames.Any()
                    ? result.MemberNames
                    : [string.Empty];

                return members.Select(member => new
                {
                    Member = member,
                    Message = result.ErrorMessage ?? "The request is invalid."
                });
            })
            .GroupBy(item => item.Member)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Message).Distinct().ToArray());
    }
}
