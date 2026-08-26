namespace MunicipalPlatform.Api.Modules.Gazette.Domain;

public sealed class GazetteTransitionException(string message) : InvalidOperationException(message);
