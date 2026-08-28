namespace MunicipalPlatform.Api.Modules.Operations.Domain;

public enum IntegrationState
{
    Configured,
    Degraded,
    Unavailable,
    NotConfigured
}

public static class IntegrationStateVocabulary
{
    // Every surface must publish the same compliance vocabulary. Serializing the enum directly
    // leaks the ordinal, and upper-casing the name drops the separator the vocabulary requires.
    public static string ToExternalState(this IntegrationState state) => state switch
    {
        IntegrationState.Configured => "CONFIGURED",
        IntegrationState.Degraded => "DEGRADED",
        IntegrationState.Unavailable => "UNAVAILABLE",
        IntegrationState.NotConfigured => "NOT_CONFIGURED",
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Estado de integração desconhecido.")
    };
}
