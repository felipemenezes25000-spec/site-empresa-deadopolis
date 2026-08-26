namespace MunicipalPlatform.Api.Modules.Gazette.Domain;

public sealed class GazetteImmutabilityException(string message) : InvalidOperationException(message);
