using System.Text;
using MunicipalPlatform.Api.Modules.Mail.Domain;
using MunicipalPlatform.Api.Modules.Mail.Services;

namespace MunicipalPlatform.Api.Tests.Mail;

public sealed class MailArchiveInspectionServiceTests
{
    private readonly MailArchiveInspectionService _service = new();

    [Fact]
    public async Task EmlInspectionRecordsOneCandidateAndStableHash()
    {
        const string eml = "From: origem@example.test\r\nTo: destino@example.test\r\nSubject: Teste\r\nMessage-ID: <1@example.test>\r\n\r\nCorpo da mensagem.";
        await using var stream = new MemoryStream(Encoding.ASCII.GetBytes(eml));

        var result = await _service.InspectAsync("EML", stream);

        Assert.Equal(1, result.CandidateMessages);
        Assert.Equal(0, result.InvalidMessages);
        Assert.Equal(stream.Length, result.SourceBytes);
        Assert.Equal(64, result.SourceSha256.Length);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task InvalidEmlIsReportedWithoutPretendingItWasImported()
    {
        await using var stream = new MemoryStream(Encoding.ASCII.GetBytes("texto sem cabecalhos nem separador"));

        var result = await _service.InspectAsync("EML", stream);
        var job = new MailMigrationJob(Guid.NewGuid(), "EML", "lote-001.eml", "arquivo@example.test");
        job.RecordLocalInspection(
            result.CandidateMessages,
            result.InvalidMessages,
            result.SourceBytes,
            result.SourceSha256,
            string.Join(" ", result.Warnings),
            DateTimeOffset.UtcNow);

        Assert.Equal(0, result.CandidateMessages);
        Assert.Equal(1, result.InvalidMessages);
        Assert.Equal("LOCAL_VALIDATION_FAILED", job.State);
        Assert.Equal(0, job.ImportedMessages);
        Assert.Equal(0, job.CandidateMessages);
        Assert.Equal(1, job.FailedMessages);
        Assert.NotNull(job.InspectedAt);
    }

    [Fact]
    public async Task MboxInspectionCountsValidAndInvalidMessagesSeparately()
    {
        const string mbox = "From sender@example.test Thu Aug 27 12:00:00 2026\n"
            + "From: sender@example.test\nTo: target@example.test\nSubject: Primeira\n\nCorpo 1\n"
            + "From sender@example.test Thu Aug 27 12:01:00 2026\n"
            + "isto nao e um cabecalho valido\n\nCorpo 2\n"
            + "From sender@example.test Thu Aug 27 12:02:00 2026\n"
            + "From: sender@example.test\nTo: target@example.test\nSubject: Terceira\n\nCorpo 3\n";
        await using var stream = new MemoryStream(Encoding.ASCII.GetBytes(mbox));

        var result = await _service.InspectAsync("MBOX", stream);

        Assert.Equal(2, result.CandidateMessages);
        Assert.Equal(1, result.InvalidMessages);
        Assert.Contains(result.Warnings, warning => warning.Contains("não passaram", StringComparison.Ordinal));
    }

    [Fact]
    public async Task LocalInspectorRejectsImapBecauseCredentialsBelongToExternalConnector()
    {
        await using var stream = new MemoryStream(Encoding.ASCII.GetBytes("irrelevant"));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.InspectAsync("IMAP", stream));

        Assert.Contains("somente EML ou MBOX", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InteractiveInspectionRejectsArchiveAboveBoundedLimit()
    {
        var bytes = new byte[MailArchiveInspectionService.MaxArchiveBytes + 1];
        await using var stream = new MemoryStream(bytes, writable: false);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => _service.InspectAsync("EML", stream));

        Assert.Contains("25 MB", exception.Message, StringComparison.Ordinal);
    }
}
