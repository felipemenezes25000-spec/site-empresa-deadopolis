using System.Security.Cryptography;
using System.Text;

namespace MunicipalPlatform.Api.Modules.Mail.Services;

public sealed record MailArchiveInspectionResult(
    int CandidateMessages,
    int InvalidMessages,
    long SourceBytes,
    string SourceSha256,
    IReadOnlyList<string> Warnings);

public sealed class MailArchiveInspectionService
{
    public const long MaxArchiveBytes = 25L * 1024 * 1024;
    private const int MaxMessages = 50_000;
    private const int MaxHeaderCharacters = 256 * 1024;
    private const int MaxWarnings = 10;

    public async Task<MailArchiveInspectionResult> InspectAsync(
        string sourceType,
        Stream source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        var normalizedType = sourceType.Trim().ToUpperInvariant();
        if (normalizedType is not ("EML" or "MBOX"))
            throw new ArgumentException("A inspeção local aceita somente EML ou MBOX.", nameof(sourceType));

        var bytes = await ReadBoundedAsync(source, cancellationToken);
        if (bytes.Length == 0) throw new InvalidDataException("O arquivo de migração está vazio.");

        var sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var text = Encoding.Latin1.GetString(bytes);
        var warnings = new List<string>();
        var (candidateMessages, invalidMessages) = normalizedType == "EML"
            ? InspectEml(text, warnings)
            : InspectMbox(text, warnings);

        if (candidateMessages == 0 && invalidMessages == 0)
        {
            invalidMessages = 1;
            AddWarning(warnings, "Nenhuma mensagem RFC 5322 reconhecível foi encontrada.");
        }

        return new MailArchiveInspectionResult(
            candidateMessages,
            invalidMessages,
            bytes.LongLength,
            sha256,
            warnings);
    }

    private static async Task<byte[]> ReadBoundedAsync(Stream source, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        var buffer = new byte[64 * 1024];
        long total = 0;
        while (true)
        {
            var read = await source.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (read == 0) break;
            total += read;
            if (total > MaxArchiveBytes)
                throw new InvalidDataException("O arquivo de migração excede o limite de 25 MB para inspeção interativa.");
            await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        return memory.ToArray();
    }

    private static (int CandidateMessages, int InvalidMessages) InspectEml(string text, List<string> warnings)
    {
        if (InspectMessage(text.AsSpan(), out var reason)) return (1, 0);
        AddWarning(warnings, $"EML inválido: {reason}");
        return (0, 1);
    }

    private static (int CandidateMessages, int InvalidMessages) InspectMbox(string text, List<string> warnings)
    {
        var boundaries = FindMboxBoundaries(text);
        if (boundaries.Count == 0)
        {
            AddWarning(warnings, "MBOX sem delimitadores de envelope “From ” no início de linha.");
            return (0, 1);
        }

        var valid = 0;
        var invalid = 0;
        var inspected = Math.Min(boundaries.Count, MaxMessages);
        for (var index = 0; index < inspected; index++)
        {
            var boundary = boundaries[index];
            var envelopeEnd = text.IndexOf('\n', boundary);
            if (envelopeEnd < 0)
            {
                invalid++;
                AddWarning(warnings, $"Mensagem {index + 1}: envelope MBOX sem conteúdo.");
                continue;
            }

            var messageStart = envelopeEnd + 1;
            var messageEnd = index + 1 < boundaries.Count ? boundaries[index + 1] : text.Length;
            if (messageEnd <= messageStart)
            {
                invalid++;
                AddWarning(warnings, $"Mensagem {index + 1}: mensagem vazia após o envelope MBOX.");
                continue;
            }

            if (!InspectMessage(text.AsSpan(messageStart, messageEnd - messageStart), out var reason))
            {
                invalid++;
                AddWarning(warnings, $"Mensagem {index + 1}: {reason}");
                continue;
            }

            valid++;
        }

        if (boundaries.Count > MaxMessages)
            AddWarning(warnings, $"A inspeção foi limitada às primeiras {MaxMessages} mensagens do arquivo.");
        if (invalid > 0)
            AddWarning(warnings, $"{invalid} mensagem(ns) não passaram pela validação estrutural local.");

        return (valid, invalid);
    }

    private static List<int> FindMboxBoundaries(string text)
    {
        var boundaries = new List<int>();
        if (text.StartsWith("From ", StringComparison.Ordinal)) boundaries.Add(0);

        var cursor = 0;
        while (cursor < text.Length)
        {
            var found = text.IndexOf("\nFrom ", cursor, StringComparison.Ordinal);
            if (found < 0) break;
            boundaries.Add(found + 1);
            cursor = found + 6;
            if (boundaries.Count > MaxMessages) break;
        }

        return boundaries;
    }

    private static bool InspectMessage(ReadOnlySpan<char> message, out string reason)
    {
        reason = string.Empty;
        if (message.IsEmpty)
        {
            reason = "mensagem vazia.";
            return false;
        }

        var headerLength = FindHeaderLength(message);
        if (headerLength <= 0)
        {
            reason = "separador entre cabeçalhos e corpo não encontrado.";
            return false;
        }
        if (headerLength > MaxHeaderCharacters)
        {
            reason = "cabeçalhos excedem 256 KB.";
            return false;
        }

        var headers = message[..headerLength].ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
        var lines = headers.Split('\n');
        var structuredHeaders = 0;
        foreach (var rawLine in lines)
        {
            if (rawLine.Length == 0 || rawLine[0] is ' ' or '\t') continue;
            var separator = rawLine.IndexOf(':');
            if (separator <= 0) continue;
            var fieldName = rawLine[..separator];
            if (fieldName.All(character => char.IsAsciiLetterOrDigit(character) || character == '-')) structuredHeaders++;
        }

        if (structuredHeaders == 0)
        {
            reason = "nenhum cabeçalho RFC reconhecível foi encontrado.";
            return false;
        }

        return true;
    }

    private static int FindHeaderLength(ReadOnlySpan<char> message)
    {
        var crlf = message.IndexOf("\r\n\r\n".AsSpan(), StringComparison.Ordinal);
        if (crlf >= 0) return crlf;
        return message.IndexOf("\n\n".AsSpan(), StringComparison.Ordinal);
    }

    private static void AddWarning(List<string> warnings, string warning)
    {
        if (warnings.Count < MaxWarnings) warnings.Add(warning);
    }
}
