using Microsoft.AspNetCore.DataProtection;
using MunicipalPlatform.Api.Modules.Identity;

namespace MunicipalPlatform.Api.Tests.Identity;

public sealed class MfaTotpServiceTests
{
    [Fact]
    public void EnrollmentSecretCanBeVerifiedWithoutStoringPlaintext()
    {
        using var directory = new TemporaryDirectory();
        var provider = DataProtectionProvider.Create(new DirectoryInfo(directory.Path));
        var service = new MfaTotpService(provider);
        var enrollment = service.CreateEnrollment("Teste", "admin.demo");
        Assert.DoesNotContain(enrollment.Secret, enrollment.ProtectedSecret, StringComparison.Ordinal);
        Assert.StartsWith("otpauth://totp/", enrollment.OtpAuthUri);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"mfa-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, true);
        }
    }
}
