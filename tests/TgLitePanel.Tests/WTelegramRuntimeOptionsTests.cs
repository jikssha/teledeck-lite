using TgLitePanel.Infrastructure.WTelegram;
using Xunit;

namespace TgLitePanel.Tests;

/// <summary>
/// WTelegramRuntimeOptions 配置测试
/// </summary>
public sealed class WTelegramRuntimeOptionsTests
{
    [Fact]
    public void DefaultValues_正确设置()
    {
        var options = new WTelegramRuntimeOptions();

        Assert.Equal(TimeSpan.FromMinutes(5), options.IdleTtl);
        Assert.Equal(TimeSpan.FromSeconds(10), options.ReapInterval);
        Assert.Equal("zh-CN", options.SystemLanguageCode);
        Assert.Equal("TgLitePanel", options.DeviceModel);
        Assert.Equal("Linux", options.SystemVersion);
        Assert.Equal("1.0", options.ApplicationVersion);
    }

    [Fact]
    public void CanSetCustomValues()
    {
        var options = new WTelegramRuntimeOptions
        {
            IdleTtl = TimeSpan.FromMinutes(10),
            ReapInterval = TimeSpan.FromSeconds(30),
            SystemLanguageCode = "en",
            DeviceModel = "CustomDevice",
            SystemVersion = "Windows",
            ApplicationVersion = "2.0"
        };

        Assert.Equal(TimeSpan.FromMinutes(10), options.IdleTtl);
        Assert.Equal(TimeSpan.FromSeconds(30), options.ReapInterval);
        Assert.Equal("en", options.SystemLanguageCode);
        Assert.Equal("CustomDevice", options.DeviceModel);
        Assert.Equal("Windows", options.SystemVersion);
        Assert.Equal("2.0", options.ApplicationVersion);
    }

    [Theory]
    [InlineData("zh-CN")]
    [InlineData("en")]
    [InlineData("ja")]
    [InlineData("ko")]
    public void SystemLanguageCode_支持多种语言代码(string langCode)
    {
        var options = new WTelegramRuntimeOptions { SystemLanguageCode = langCode };
        Assert.Equal(langCode, options.SystemLanguageCode);
    }
}
