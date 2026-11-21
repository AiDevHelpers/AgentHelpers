using AiDevHelpers.AgentHelpers.Anthropic;
using AiDevHelpers.AgentHelpers.Core.Models;
using AiDevHelpers.AgentHelpers.DeepSeek;
using AiDevHelpers.AgentHelpers.OpenSource;

namespace AiDevHelpers.AgentHelpers.Core.Tests;

public class VariantDetectionTests
{
    [Theory]
    [InlineData("claude-haiku-4-5")]
    [InlineData("CLAUDE-HAIKU-4-5")]
    public void Anthropic_HaikuHint_UsesHaikuFormat(string hint)
    {
        var adapter = new AnthropicAgentAdapter();
        var caller = new CallerInfo { ModelHint = hint };

        Assert.Equal(ContextFormat.AnthropicHaiku, adapter.GetFormat(caller));
    }

    [Fact]
    public void Anthropic_OpusHint_UsesOpusFormat()
    {
        var adapter = new AnthropicAgentAdapter();
        var caller = new CallerInfo { ModelHint = "claude-opus-4-1" };

        Assert.Equal(ContextFormat.AnthropicOpus, adapter.GetFormat(caller));
    }

    [Fact]
    public void Anthropic_SonnetHint_UsesSonnetFormat()
    {
        var adapter = new AnthropicAgentAdapter();
        var caller = new CallerInfo { ModelHint = "claude-sonnet-4-6" };

        Assert.Equal(ContextFormat.AnthropicSonnet, adapter.GetFormat(caller));
    }

    [Theory]
    [InlineData("deepseek-r1")]
    [InlineData("deepseek-reasoner")]
    public void DeepSeek_R1Hint_UsesR1Format(string hint)
    {
        var adapter = new DeepSeekAgentAdapter();
        var caller = new CallerInfo { ModelHint = hint };

        Assert.Equal(ContextFormat.DeepSeekR1, adapter.GetFormat(caller));
    }

    [Fact]
    public void DeepSeek_CoderHint_UsesV3Format()
    {
        // Coder shares the V3 ContextFormat; the variant changes the adapted output
        // (per-file language and path annotations), so compare the rendered context.
        var adapter = new DeepSeekAgentAdapter();
        var coder = new CallerInfo { ModelHint = "deepseek-coder-v2" };
        var v3 = new CallerInfo { ModelHint = "deepseek-chat" };
        const string raw = "Summary:\nhello\n\n```\n// a.cs\npublic class A {}\n```\n\nwhat is a.cs?";

        Assert.Equal(ContextFormat.DeepSeekV3, adapter.GetFormat(coder));
        Assert.NotEqual(
            adapter.AdaptContext(raw, v3, ContextFormat.DeepSeekV3),
            adapter.AdaptContext(raw, coder, ContextFormat.DeepSeekV3));
    }

    [Fact]
    public void OpenSource_GemmaHint_UsesGemmaFormat()
    {
        var adapter = new OpenSourceAgentAdapter();
        var caller = new CallerInfo { ModelHint = "gemma2" };

        Assert.Equal(ContextFormat.Gemma, adapter.GetFormat(caller));
    }

    [Theory]
    [InlineData("mistral-nemo")]
    [InlineData("mixtral-8x7b")]
    public void OpenSource_MistralHint_UsesMistralFormat(string hint)
    {
        var adapter = new OpenSourceAgentAdapter();
        var caller = new CallerInfo { ModelHint = hint };

        Assert.Equal(ContextFormat.Mistral, adapter.GetFormat(caller));
    }

    [Fact]
    public void OpenSource_LlamaHint_UsesLlamaFormat()
    {
        var adapter = new OpenSourceAgentAdapter();
        var caller = new CallerInfo { ModelHint = "llama3.2" };

        Assert.Equal(ContextFormat.Llama, adapter.GetFormat(caller));
    }
}
