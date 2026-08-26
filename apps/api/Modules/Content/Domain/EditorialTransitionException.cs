namespace MunicipalPlatform.Api.Modules.Content.Domain;

public sealed class EditorialTransitionException(string message) : InvalidOperationException(message);
