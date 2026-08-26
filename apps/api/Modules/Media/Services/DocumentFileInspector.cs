using System.IO.Compression;
using System.Text;

namespace MunicipalPlatform.Api.Modules.Media.Services;

public sealed record DetectedDocumentFile(string MimeType, string Extension, string DocumentType);

public static class DocumentFileInspector
{
    public const long MaxBytes = 25L * 1024 * 1024;

    public static DetectedDocumentFile? Detect(ReadOnlyMemory<byte> content, string fileName)
    {
        var bytes = content.Span;
        var extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        if (bytes.Length >= 5 && Encoding.ASCII.GetString(bytes[..5]) == "%PDF-" && extension == "pdf")
            return new("application/pdf", "pdf", "PDF");
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF && extension is "jpg" or "jpeg")
            return new("image/jpeg", "jpg", "IMAGE");
        if (bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) && extension == "png")
            return new("image/png", "png", "IMAGE");
        if (bytes.Length >= 12
            && Encoding.ASCII.GetString(bytes[..4]) == "RIFF"
            && Encoding.ASCII.GetString(bytes.Slice(8, 4)) == "WEBP"
            && extension == "webp")
            return new("image/webp", "webp", "IMAGE");

        if (bytes.Length >= 4 && bytes[..4].SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }))
            return DetectOpenXml(content, extension);

        if (bytes.Length >= 8
            && bytes[..8].SequenceEqual(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }))
        {
            return extension switch
            {
                "doc" => new("application/msword", "doc", "OFFICE"),
                "xls" => new("application/vnd.ms-excel", "xls", "OFFICE"),
                "ppt" => new("application/vnd.ms-powerpoint", "ppt", "OFFICE"),
                _ => null
            };
        }

        return null;
    }

    public static bool IsDeclaredMimeCompatible(string? declaredMime, string detectedMime)
    {
        if (string.IsNullOrWhiteSpace(declaredMime)
            || declaredMime.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase)
            || declaredMime.Equals("binary/octet-stream", StringComparison.OrdinalIgnoreCase))
            return true;
        return declaredMime.Equals(detectedMime, StringComparison.OrdinalIgnoreCase)
            || detectedMime == "image/jpeg" && declaredMime.Equals("image/jpg", StringComparison.OrdinalIgnoreCase);
    }

    private static DetectedDocumentFile? DetectOpenXml(ReadOnlyMemory<byte> content, string extension)
    {
        try
        {
            using var stream = new MemoryStream(content.ToArray(), writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            if (archive.GetEntry("[Content_Types].xml") is null)
                return null;
            var names = archive.Entries.Select(entry => entry.FullName).ToArray();
            return extension switch
            {
                "docx" when names.Any(name => name.StartsWith("word/", StringComparison.OrdinalIgnoreCase)) =>
                    new("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "docx", "OFFICE"),
                "xlsx" when names.Any(name => name.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)) =>
                    new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx", "OFFICE"),
                "pptx" when names.Any(name => name.StartsWith("ppt/", StringComparison.OrdinalIgnoreCase)) =>
                    new("application/vnd.openxmlformats-officedocument.presentationml.presentation", "pptx", "OFFICE"),
                _ => null
            };
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }
}
