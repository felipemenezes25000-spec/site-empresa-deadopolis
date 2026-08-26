namespace MunicipalPlatform.Api.Platform.Storage;

public interface IObjectStorageProvider
{
    string State { get; }
    string Description { get; }
    Task SaveAsync(string objectKey, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default);
    Task<byte[]?> ReadAsync(string objectKey, CancellationToken cancellationToken = default);
}

public sealed class LocalObjectStorageProvider(IConfiguration configuration) : IObjectStorageProvider
{
    private readonly string _root = Path.GetFullPath(configuration["Storage:LocalRoot"]
        ?? Path.Combine(AppContext.BaseDirectory, ".data", "objects"));

    public string State => "DEVELOPMENT_ONLY";
    public string Description => "Filesystem local habilitado somente para desenvolvimento, testes ou apresentação.";

    public async Task SaveAsync(string objectKey, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
    {
        var path = Resolve(objectKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, content.ToArray(), cancellationToken);
    }

    public async Task<byte[]?> ReadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var path = Resolve(objectKey);
        return File.Exists(path) ? await File.ReadAllBytesAsync(path, cancellationToken) : null;
    }

    private string Resolve(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey)) throw new ArgumentException("Object key obrigatória.", nameof(objectKey));
        var relative = objectKey.Replace('\\', '/').TrimStart('/');
        if (relative.Split('/').Any(segment => segment is ".." or ".")) throw new InvalidOperationException("Object key inválida.");
        var fullPath = Path.GetFullPath(Path.Combine(_root, relative.Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = _root.EndsWith(Path.DirectorySeparatorChar) ? _root : _root + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootPrefix, StringComparison.Ordinal) && !string.Equals(fullPath, _root, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Object key escapou do diretório permitido.");
        }
        return fullPath;
    }
}

public sealed class NotConfiguredObjectStorageProvider : IObjectStorageProvider
{
    public string State => "NOT_CONFIGURED";
    public string Description => "Storage de produção não foi configurado. Configure S3 compatível e secrets antes do go-live.";

    public Task SaveAsync(string objectKey, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default) =>
        throw new ExternalProviderNotConfiguredException("Storage S3/compatível está NOT_CONFIGURED.");

    public Task<byte[]?> ReadAsync(string objectKey, CancellationToken cancellationToken = default) =>
        throw new ExternalProviderNotConfiguredException("Storage S3/compatível está NOT_CONFIGURED.");
}

public sealed class ExternalProviderNotConfiguredException(string message) : InvalidOperationException(message);
