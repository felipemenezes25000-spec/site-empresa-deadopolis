using System.Collections;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MunicipalPlatform.Api.Modules.Gazette.Domain;
using QRCoder;

namespace MunicipalPlatform.Api.Modules.Gazette.Services;

public sealed record GazetteActInput(string Title, string Body, string? Organization = null, string? LegalReference = null);
public sealed record GazetteSectionInput(string Title, IReadOnlyList<GazetteActInput> Acts);
public sealed record GazetteComposition(IReadOnlyList<GazetteSectionInput> Sections);
public sealed record GazetteDocumentResult(byte[] PdfBytes, string Sha256, string ContentSha256, string VerificationCode);

public sealed class GazetteDocumentService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public string NormalizeComposition(GazetteComposition composition)
    {
        ArgumentNullException.ThrowIfNull(composition);
        if (composition.Sections is null || composition.Sections.Count is 0 or > 80)
        {
            throw new ArgumentException("A edição precisa possuir entre 1 e 80 seções.", nameof(composition));
        }

        var sections = composition.Sections.Select(section =>
        {
            var title = RequireText(section.Title, 160, "Título da seção");
            if (section.Acts is null || section.Acts.Count is 0 or > 250)
            {
                throw new ArgumentException($"A seção '{title}' precisa possuir entre 1 e 250 atos.", nameof(composition));
            }

            var acts = section.Acts.Select(act => new GazetteActInput(
                RequireText(act.Title, 220, "Título do ato"),
                RequireText(act.Body, 50_000, "Conteúdo do ato"),
                OptionalText(act.Organization, 180),
                OptionalText(act.LegalReference, 300))).ToArray();
            return new GazetteSectionInput(title, acts);
        }).ToArray();

        return JsonSerializer.Serialize(new GazetteComposition(sections), JsonOptions);
    }

    public GazetteComposition ParseComposition(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("A composição do Diário é obrigatória.", nameof(json));
        }

        var composition = JsonSerializer.Deserialize<GazetteComposition>(json, JsonOptions)
            ?? throw new ArgumentException("A composição do Diário é inválida.", nameof(json));
        return JsonSerializer.Deserialize<GazetteComposition>(NormalizeComposition(composition), JsonOptions)!;
    }

    public GazetteDocumentResult Generate(GazetteEdition edition, string publicVerificationBaseUrl)
    {
        ArgumentNullException.ThrowIfNull(edition);
        var composition = ParseComposition(edition.CompositionJson);
        var canonicalSnapshot = JsonSerializer.Serialize(new
        {
            edition.Number,
            edition.Year,
            type = edition.Type.ToString().ToUpperInvariant(),
            publicationDate = edition.PublicationDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            composition
        }, JsonOptions);
        var contentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalSnapshot))).ToLowerInvariant();
        var verificationCode = contentHash[..20];
        var verificationUrl = $"{publicVerificationBaseUrl.TrimEnd('/')}/verificar/{verificationCode}";
        var lines = BuildLines(edition, composition, contentHash, verificationCode);
        var pdf = BuildPdf(lines, verificationUrl);
        var pdfHash = Convert.ToHexString(SHA256.HashData(pdf)).ToLowerInvariant();
        return new GazetteDocumentResult(pdf, pdfHash, contentHash, verificationCode);
    }

    private static List<string> BuildLines(
        GazetteEdition edition,
        GazetteComposition composition,
        string contentHash,
        string verificationCode)
    {
        var lines = new List<string>
        {
            "PREFEITURA MUNICIPAL DE DEODÁPOLIS - MS",
            "DIÁRIO OFICIAL ELETRÔNICO",
            $"Edição {edition.Number}/{edition.Year} · {edition.Type}",
            $"Data de publicação: {edition.PublicationDate:dd/MM/yyyy}",
            $"Código de verificação: {verificationCode}",
            $"SHA-256 do conteúdo canônico: {contentHash}",
            "A autenticidade e o SHA-256 do arquivo podem ser conferidos no portal oficial.",
            string.Empty,
            "SUMÁRIO"
        };

        foreach (var section in composition.Sections)
        {
            lines.Add($"• {section.Title}");
            foreach (var act in section.Acts)
            {
                lines.Add($"  - {act.Title}");
            }
        }

        foreach (var section in composition.Sections)
        {
            lines.Add(string.Empty);
            lines.Add(section.Title.ToUpperInvariant());
            lines.Add(new string('-', Math.Min(72, section.Title.Length + 8)));
            foreach (var act in section.Acts)
            {
                lines.Add(act.Title);
                if (!string.IsNullOrWhiteSpace(act.Organization)) lines.Add($"Órgão: {act.Organization}");
                if (!string.IsNullOrWhiteSpace(act.LegalReference)) lines.Add($"Base legal: {act.LegalReference}");
                foreach (var line in Wrap(act.Body, 92)) lines.Add(line);
                lines.Add(string.Empty);
            }
        }

        return lines;
    }

    private static byte[] BuildPdf(IReadOnlyList<string> lines, string verificationUrl)
    {
        const int linesPerPage = 43;
        var pageChunks = lines.Chunk(linesPerPage).ToArray();
        if (pageChunks.Length == 0) pageChunks = [Array.Empty<string>()];

        var objectBodies = new Dictionary<int, string>();
        objectBodies[1] = "<< /Type /Catalog /Pages 2 0 R >>";
        var pageRefs = Enumerable.Range(0, pageChunks.Length).Select(index => $"{4 + (index * 2)} 0 R");
        objectBodies[2] = $"<< /Type /Pages /Kids [{string.Join(' ', pageRefs)}] /Count {pageChunks.Length} >>";
        objectBodies[3] = "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>";

        for (var index = 0; index < pageChunks.Length; index++)
        {
            var pageObject = 4 + (index * 2);
            var contentObject = pageObject + 1;
            objectBodies[pageObject] = $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentObject} 0 R >>";
            var stream = BuildPageStream(pageChunks[index], index + 1, pageChunks.Length, index == 0 ? verificationUrl : null);
            var streamLength = Encoding.Latin1.GetByteCount(stream);
            objectBodies[contentObject] = $"<< /Length {streamLength} >>\nstream\n{stream}\nendstream";
        }

        using var output = new MemoryStream();
        WriteLatin1(output, "%PDF-1.4\n%âãÏÓ\n");
        var offsets = new long[objectBodies.Count + 1];
        for (var objectNumber = 1; objectNumber <= objectBodies.Count; objectNumber++)
        {
            offsets[objectNumber] = output.Position;
            WriteLatin1(output, $"{objectNumber} 0 obj\n{objectBodies[objectNumber]}\nendobj\n");
        }

        var xrefOffset = output.Position;
        WriteLatin1(output, $"xref\n0 {objectBodies.Count + 1}\n0000000000 65535 f \n");
        for (var objectNumber = 1; objectNumber <= objectBodies.Count; objectNumber++)
        {
            WriteLatin1(output, $"{offsets[objectNumber]:D10} 00000 n \n");
        }

        WriteLatin1(output, $"trailer\n<< /Size {objectBodies.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        return output.ToArray();
    }

    private static string BuildPageStream(IReadOnlyList<string> lines, int page, int pageCount, string? verificationUrl)
    {
        var builder = new StringBuilder();
        builder.Append("BT /F1 10 Tf 48 790 Td 14 TL\n");
        foreach (var line in lines)
        {
            builder.Append('(').Append(EscapePdf(line)).Append(") Tj T*\n");
        }
        builder.Append("ET\n");
        builder.Append($"BT /F1 8 Tf 48 28 Td (Página {page} de {pageCount}) Tj ET\n");

        if (!string.IsNullOrWhiteSpace(verificationUrl))
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(verificationUrl, QRCodeGenerator.ECCLevel.Q);
            AppendQr(builder, data.ModuleMatrix, 445, 650, 2.2m);
            builder.Append($"BT /F1 7 Tf 420 638 Td ({EscapePdf("Verifique no portal oficial")}) Tj ET\n");
        }

        return builder.ToString();
    }

    private static void AppendQr(StringBuilder builder, IReadOnlyList<BitArray> matrix, decimal originX, decimal originY, decimal moduleSize)
    {
        builder.Append("0 0 0 rg\n");
        for (var row = 0; row < matrix.Count; row++)
        {
            for (var column = 0; column < matrix[row].Count; column++)
            {
                if (!matrix[row][column]) continue;
                var x = originX + (column * moduleSize);
                var y = originY - (row * moduleSize);
                builder.AppendFormat(CultureInfo.InvariantCulture, "{0:0.##} {1:0.##} {2:0.##} {2:0.##} re f\n", x, y, moduleSize);
            }
        }
    }

    private static IEnumerable<string> Wrap(string value, int width)
    {
        var words = value.Replace("\r", string.Empty, StringComparison.Ordinal).Split([' ', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var current = new StringBuilder();
        foreach (var word in words)
        {
            if (current.Length > 0 && current.Length + word.Length + 1 > width)
            {
                yield return current.ToString();
                current.Clear();
            }
            if (current.Length > 0) current.Append(' ');
            current.Append(word);
        }
        if (current.Length > 0) yield return current.ToString();
    }

    private static string EscapePdf(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            var safe = character <= 255 ? character : '?';
            if (safe is '(' or ')' or '\\') builder.Append('\\');
            builder.Append(safe);
        }
        return builder.ToString();
    }

    private static void WriteLatin1(Stream stream, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string RequireText(string value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{label} é obrigatório.");
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new ArgumentException($"{label} deve possuir até {maxLength} caracteres.");
        return normalized;
    }

    private static string? OptionalText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new ArgumentException($"Campo opcional deve possuir até {maxLength} caracteres.");
        return normalized;
    }
}
