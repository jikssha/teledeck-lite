using TgLitePanel.Host.Services;
using Xunit;

namespace TgLitePanel.Tests;

/// <summary>
/// PasswordHasher 服务测试
/// </summary>
public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_生成有效的哈希格式()
    {
        var hash = _hasher.Hash("test-password");

        Assert.NotNull(hash);
        Assert.StartsWith("v1$", hash);

        var parts = hash.Split('$');
        Assert.Equal(4, parts.Length);
        Assert.Equal("v1", parts[0]);
        Assert.True(int.TryParse(parts[1], out var iterations));
        Assert.Equal(150_000, iterations);
    }

    [Fact]
    public void Hash_相同密码生成不同哈希()
    {
        var hash1 = _hasher.Hash("same-password");
        var hash2 = _hasher.Hash("same-password");

        // 由于使用随机盐，相同密码应生成不同哈希
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Hash_空密码抛出异常()
    {
        Assert.Throws<ArgumentException>(() => _hasher.Hash(""));
        Assert.Throws<ArgumentException>(() => _hasher.Hash(null!));
    }

    [Fact]
    public void Verify_正确密码验证成功()
    {
        var password = "my-secure-password";
        var hash = _hasher.Hash(password);

        Assert.True(_hasher.Verify(password, hash));
    }

    [Fact]
    public void Verify_错误密码验证失败()
    {
        var hash = _hasher.Hash("correct-password");

        Assert.False(_hasher.Verify("wrong-password", hash));
    }

    [Fact]
    public void Verify_空密码返回false()
    {
        var hash = _hasher.Hash("some-password");

        Assert.False(_hasher.Verify("", hash));
        Assert.False(_hasher.Verify(null!, hash));
    }

    [Fact]
    public void Verify_无效哈希格式返回false()
    {
        Assert.False(_hasher.Verify("password", "invalid-hash"));
        Assert.False(_hasher.Verify("password", "v2$100$salt$key"));
        Assert.False(_hasher.Verify("password", ""));
        Assert.False(_hasher.Verify("password", null!));
    }

    [Fact]
    public void Verify_损坏的Base64返回false()
    {
        Assert.False(_hasher.Verify("password", "v1$150000$not-base64$also-not-base64"));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("a-longer-password-with-special-chars!@#$%^&*()")]
    [InlineData("密码测试中文")]
    [InlineData("password with spaces")]
    public void Hash_支持各种密码格式(string password)
    {
        var hash = _hasher.Hash(password);
        Assert.True(_hasher.Verify(password, hash));
    }
}
