using RaceDay.Api.Auth;

namespace RaceDay.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_DoesNotContainThePlainPassword()
    {
        var hash = PasswordHasher.Hash("MySecret123!");
        Assert.DoesNotContain("MySecret123!", hash);
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = PasswordHasher.Hash("MySecret123!");
        Assert.True(PasswordHasher.Verify("MySecret123!", hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = PasswordHasher.Hash("MySecret123!");
        Assert.False(PasswordHasher.Verify("WrongPassword", hash));
    }

    [Fact]
    public void Hash_SamePasswordTwice_GivesDifferentHashes()
    {
        Assert.NotEqual(PasswordHasher.Hash("MySecret123!"), PasswordHasher.Hash("MySecret123!"));
    }

    [Fact]
    public void Verify_MalformedHash_ReturnsFalse()
    {
        Assert.False(PasswordHasher.Verify("anything", "not-a-valid-hash"));
    }
}