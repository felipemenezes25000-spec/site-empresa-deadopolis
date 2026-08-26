using MunicipalPlatform.Api.Modules.Identity.Domain;

namespace MunicipalPlatform.Api.Tests.Identity;

public sealed class UserAccountSecurityTests
{
    [Fact]
    public void Five_failed_logins_lock_account_temporarily()
    {
        var user = new UserAccount(Guid.NewGuid(), "admin", "Admin", "SUPER_ADMIN", "hash");
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < 5; i++) user.RecordFailedLogin(now.AddSeconds(i));
        Assert.True(user.IsLocked(now.AddMinutes(1)));
        Assert.False(user.IsLocked(now.AddMinutes(16)));
    }

    [Fact]
    public void Revoking_sessions_changes_security_version()
    {
        var user = new UserAccount(Guid.NewGuid(), "admin", "Admin", "SUPER_ADMIN", "hash");
        var before = user.SessionVersion; user.RevokeSessions(); Assert.Equal(before + 1, user.SessionVersion);
    }
}
