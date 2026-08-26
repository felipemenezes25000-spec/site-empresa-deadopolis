namespace MunicipalPlatform.Api.Modules.Support.Domain;

public readonly record struct SlaDeadlines(
    DateTimeOffset FirstResponseDueAt,
    DateTimeOffset ResolutionDueAt);
