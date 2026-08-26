namespace MunicipalPlatform.Api.Platform.Tenancy;

public sealed class TenantResolutionException(string message) : InvalidOperationException(message);
