using System.Text.RegularExpressions;
using VfdControl.Domain.Enums;

namespace VfdControl.Domain.Rules;

public sealed record StringMatchRule(
    StringMatchMode Mode,
    string? ExpectedValue,
    IReadOnlyCollection<string> Values,
    bool IsCaseSensitive = true,
    bool AffectsFinalConclusion = true)
{
    public static StringMatchRule Exact(string expectedValue, bool isCaseSensitive = true)
    {
        return new StringMatchRule(StringMatchMode.Exact, expectedValue, [], isCaseSensitive);
    }

    public static StringMatchRule AllowedValues(IReadOnlyCollection<string> allowedValues, bool isCaseSensitive = true)
    {
        return new StringMatchRule(StringMatchMode.AllowedValues, null, allowedValues, isCaseSensitive);
    }

    public static StringMatchRule Contains(string expectedValue, bool isCaseSensitive = true)
    {
        return new StringMatchRule(StringMatchMode.Contains, expectedValue, [], isCaseSensitive);
    }

    public static StringMatchRule Prefix(string expectedValue, bool isCaseSensitive = true)
    {
        return new StringMatchRule(StringMatchMode.Prefix, expectedValue, [], isCaseSensitive);
    }

    public static StringMatchRule Suffix(string expectedValue, bool isCaseSensitive = true)
    {
        return new StringMatchRule(StringMatchMode.Suffix, expectedValue, [], isCaseSensitive);
    }

    public static StringMatchRule Regex(string pattern, bool isCaseSensitive = true)
    {
        return new StringMatchRule(StringMatchMode.Regex, pattern, [], isCaseSensitive);
    }

    public RuleEvaluationResult Evaluate(string actualValue)
    {
        var matched = Mode switch
        {
            StringMatchMode.Exact => TextEquals(actualValue, ExpectedValue),
            StringMatchMode.AllowedValues => Values.Any(value => TextEquals(actualValue, value)),
            StringMatchMode.Contains => ContainsText(actualValue, ExpectedValue),
            StringMatchMode.Prefix => StartsWithText(actualValue, ExpectedValue),
            StringMatchMode.Suffix => EndsWithText(actualValue, ExpectedValue),
            StringMatchMode.Regex => RegexMatches(actualValue, ExpectedValue),
            _ => false
        };

        return new RuleEvaluationResult(
            matched ? Conclusion.Pass : Conclusion.Fail,
            matched ? "String value matched rule." : "String value did not match rule.",
            AffectsFinalConclusion);
    }

    private StringComparison Comparison => IsCaseSensitive
        ? StringComparison.Ordinal
        : StringComparison.OrdinalIgnoreCase;

    private bool TextEquals(string actualValue, string? expectedValue)
    {
        return expectedValue is not null && string.Equals(actualValue, expectedValue, Comparison);
    }

    private bool ContainsText(string actualValue, string? expectedValue)
    {
        return expectedValue is not null && actualValue.Contains(expectedValue, Comparison);
    }

    private bool StartsWithText(string actualValue, string? expectedValue)
    {
        return expectedValue is not null && actualValue.StartsWith(expectedValue, Comparison);
    }

    private bool EndsWithText(string actualValue, string? expectedValue)
    {
        return expectedValue is not null && actualValue.EndsWith(expectedValue, Comparison);
    }

    private bool RegexMatches(string actualValue, string? pattern)
    {
        if (pattern is null)
        {
            return false;
        }

        var options = IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
        return System.Text.RegularExpressions.Regex.IsMatch(actualValue, pattern, options);
    }
}

public enum StringMatchMode
{
    Exact,
    AllowedValues,
    Contains,
    Prefix,
    Suffix,
    Regex
}
