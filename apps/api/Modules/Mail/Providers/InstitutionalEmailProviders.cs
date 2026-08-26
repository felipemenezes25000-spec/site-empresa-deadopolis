namespace MunicipalPlatform.Api.Modules.Mail.Providers;

public sealed record MailboxProvisioningRequest(string Address, string DisplayName, int QuotaMegabytes);
public sealed record MailboxProvisioningResult(string Status, string? ExternalId, string Detail);

public interface IInstitutionalEmailProvider
{
    string State { get; }
    string Description { get; }
    Task<MailboxProvisioningResult> ProvisionAsync(MailboxProvisioningRequest request, CancellationToken cancellationToken = default);
}

public sealed class DemoInstitutionalEmailProvider : IInstitutionalEmailProvider
{
    public string State => "DEMO_ONLY";
    public string Description => "Provider de e-mail simulado exclusivamente no ambiente de apresentação; nenhuma caixa externa é criada.";
    public Task<MailboxProvisioningResult> ProvisionAsync(MailboxProvisioningRequest request, CancellationToken cancellationToken = default) { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult(new MailboxProvisioningResult("DEMO_ONLY", $"demo-{Guid.NewGuid():N}", Description)); }
}

public sealed class NotConfiguredInstitutionalEmailProvider : IInstitutionalEmailProvider
{
    public string State => "NOT_CONFIGURED";
    public string Description => "Provider institucional de e-mail e credenciais de produção não configurados.";
    public Task<MailboxProvisioningResult> ProvisionAsync(MailboxProvisioningRequest request, CancellationToken cancellationToken = default) => Task.FromResult(new MailboxProvisioningResult("NOT_CONFIGURED", null, Description));
}
