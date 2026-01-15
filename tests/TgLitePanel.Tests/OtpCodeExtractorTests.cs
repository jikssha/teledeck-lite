using System.Text.Json;
using TgLitePanel.Core.Abstractions.Utils;
using Xunit;

namespace TgLitePanel.Tests;

public sealed class OtpCodeExtractorTests
{
    [Fact]
    public void TryExtract_能提取4到8位数字()
    {
        Assert.True(OtpCodeExtractor.TryExtract("Telegram code: 123456", out var code));
        Assert.Equal("123456", code);
    }

    [Fact]
    public void TryExtract_忽略不足4位数字()
    {
        Assert.False(OtpCodeExtractor.TryExtract("code 123", out _));
    }

    [Fact]
    public void TryExtractPlainText_能从messageText结构提取文本()
    {
        var json = """
                   {
                     "@type": "message",
                     "content": {
                       "@type": "messageText",
                       "text": { "text": "Your login code is 654321" }
                     }
                   }
                   """;

        using var doc = JsonDocument.Parse(json);
        Assert.True(SystemMessageTextExtractor.TryExtractPlainText(doc.RootElement, out var text));
        Assert.Equal("Your login code is 654321", text);
    }
}
