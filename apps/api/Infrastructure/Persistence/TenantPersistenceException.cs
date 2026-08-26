namespace MunicipalPlatform.Api.Infrastructure.Persistence;

public sealed class TenantPersistenceException(string message) : InvalidOperationException(message);
