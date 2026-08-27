using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MunicipalPlatform.Api.Tests.Api;

public sealed class MailArchiveContractTests : IClassFixture<MunicipalApiFactory>
{
    private readonly MunicipalApiFactory _factory;

    public MailArchiveContractTests(MunicipalApiFactory factory) => _factory = factory;

    [Fact]
    public async Task AuthenticatedMailAdminCanInspectEmlWithoutPretendingMessagesWereImported()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");
        await LoginAsync(client);

        using var createResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/admin/mail/migration-jobs", UriKind.Relative),
            new
            {
                sourceType = "EML",
                sourceReference = "lote-001.eml",
                targetAddress = "arquivo@example.test"
            });
        Assert.Equal(HttpStatusCode.Accepted, createResponse.StatusCode);

        using var createdDocument = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var jobId = createdDocument.RootElement.GetProperty("job").GetProperty("id").GetGuid();

        const string eml = "From: origem@example.test\r\n"
            + "To: destino@example.test\r\n"
            + "Subject: Teste de contrato\r\n"
            + "Message-ID: <contract-1@example.test>\r\n"
            + "\r\n"
            + "Corpo sintético para validar o endpoint de inspeção.";
        var bytes = Encoding.ASCII.GetBytes(eml);

        using var multipart = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("message/rfc822");
        multipart.Add(fileContent, "file", "lote-001.eml");

        using var inspectResponse = await client.PostAsync(
            new Uri($"/api/v1/admin/mail/migration-jobs/{jobId}/inspect", UriKind.Relative),
            multipart);
        Assert.Equal(HttpStatusCode.OK, inspectResponse.StatusCode);

        using var inspectedDocument = JsonDocument.Parse(await inspectResponse.Content.ReadAsStringAsync());
        var inspectedRoot = inspectedDocument.RootElement;
        var inspectedJob = inspectedRoot.GetProperty("job");
        Assert.False(inspectedRoot.GetProperty("importExecuted").GetBoolean());
        Assert.Equal("VALIDATED_LOCAL", inspectedJob.GetProperty("state").GetString());
        Assert.Equal(1, inspectedJob.GetProperty("candidateMessages").GetInt32());
        Assert.Equal(0, inspectedJob.GetProperty("importedMessages").GetInt32());
        Assert.Equal(0, inspectedJob.GetProperty("failedMessages").GetInt32());
        Assert.Equal(bytes.LongLength, inspectedJob.GetProperty("sourceBytes").GetInt64());
        Assert.Equal(64, inspectedJob.GetProperty("sourceSha256").GetString()?.Length);
        Assert.NotEqual(JsonValueKind.Null, inspectedJob.GetProperty("inspectedAt").ValueKind);

        using var listResponse = await client.GetAsync(new Uri("/api/v1/admin/mail/migration-jobs", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        using var listDocument = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        var persistedJob = listDocument.RootElement
            .EnumerateArray()
            .Single(item => item.GetProperty("id").GetGuid() == jobId);

        Assert.Equal("VALIDATED_LOCAL", persistedJob.GetProperty("state").GetString());
        Assert.Equal(1, persistedJob.GetProperty("candidateMessages").GetInt32());
        Assert.Equal(0, persistedJob.GetProperty("importedMessages").GetInt32());
        Assert.Equal(bytes.LongLength, persistedJob.GetProperty("sourceBytes").GetInt64());
        Assert.Equal(inspectedJob.GetProperty("sourceSha256").GetString(), persistedJob.GetProperty("sourceSha256").GetString());
    }

    private static async Task LoginAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { username = "admin.demo", password = "Demo-Local-2026!" });
        response.EnsureSuccessStatusCode();
    }
}
