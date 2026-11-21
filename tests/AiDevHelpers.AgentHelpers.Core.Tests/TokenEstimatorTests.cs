using AiDevHelpers.AgentHelpers.Core.Services;

namespace AiDevHelpers.AgentHelpers.Core.Tests;

public class TokenEstimatorTests
{
    [Fact]
    public void Estimate_ReturnsZero_ForNullOrEmpty()
    {
        Assert.Equal(0, TokenEstimator.Estimate(null));
        Assert.Equal(0, TokenEstimator.Estimate(string.Empty));
    }

    [Fact]
    public void Estimate_UsesFourCharsPerToken()
    {
        Assert.Equal(2, TokenEstimator.Estimate("abcdefgh"));
        Assert.Equal(2, TokenEstimator.Estimate("abcdefghi"));
    }

    [Fact]
    public void FitsInBudget_IsTrue_WhenEstimateWithinBudget()
    {
        Assert.True(TokenEstimator.FitsInBudget("abcdefgh", 2));
        Assert.False(TokenEstimator.FitsInBudget("abcdefghi", 1));
    }

    [Fact]
    public void TrimToBudget_ReturnsInput_WhenItFits()
    {
        const string text = "short text";
        Assert.Equal(text, TokenEstimator.TrimToBudget(text, 100));
    }

    [Fact]
    public void TrimToBudget_TruncatesAtNewline_AndAppendsNotice()
    {
        const string text = "line one\nline two\nline three is much longer than the budget allows";

        string result = TokenEstimator.TrimToBudget(text, 6);

        Assert.StartsWith("line one\nline two", result);
        Assert.EndsWith("[...context trimmed to fit token budget...]", result);
        Assert.DoesNotContain("line three", result);
    }
}
