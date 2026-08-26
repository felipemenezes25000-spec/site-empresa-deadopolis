using System.IO.Compression;
using MunicipalPlatform.Api.Modules.Media.Services;

namespace MunicipalPlatform.Api.Tests.Media;

public sealed class DocumentFileInspectorTests
{
    [Fact]
    public void DetectRecognizesOpenXmlStructureInsteadOfZipSignatureAlone()
    {
        var bytes = CreateOpenXml("word/document.xml");

        var detected = DocumentFileInspector.Detect(bytes, "official.docx");
        var renamed = DocumentFileInspector.Detect(bytes, "official.xlsx");

        Assert.NotNull(detected);
        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", detected.MimeType);
        Assert.Null(renamed);
    }

    [Fact]
    public void DetectRejectsPdfRenamedAsOfficeDocument()
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes("%PDF-1.7\nbody");

        Assert.Null(DocumentFileInspector.Detect(bytes, "document.docx"));
    }

    private static byte[] CreateOpenXml(string partName)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            archive.CreateEntry("[Content_Types].xml");
            archive.CreateEntry(partName);
        }
        return output.ToArray();
    }
}
