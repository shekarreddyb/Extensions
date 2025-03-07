using FluentValidation;
using System;

public static class CustomValidationExtensions
{
    public static IRuleBuilderOptions<T, string> MustBeValidUrl<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Must((_, url) => IsValidUrl(url))
                          .WithMessage("Invalid URL format. Must be a valid absolute URL starting with http:// or https://.");
    }

    private static bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}