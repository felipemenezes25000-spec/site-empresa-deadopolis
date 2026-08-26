using MunicipalPlatform.Api.Modules.Content.Domain;

namespace MunicipalPlatform.Api.Tests.Content;

public sealed class EditorialWorkflowTests
{
    [Fact]
    public void SubmitForReviewTransitionsDraftAndRecordsActor()
    {
        var article = NewsArticle.Create(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Mutirão de serviços municipais",
            "mutirao-de-servicos-municipais",
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var reviewer = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        article.SubmitForReview(reviewer, new DateTimeOffset(2026, 8, 25, 13, 0, 0, TimeSpan.Zero));

        Assert.Equal(EditorialStatus.InReview, article.Status);
        Assert.Equal(reviewer, article.UpdatedBy);
        Assert.Equal(1, article.Version);
    }

    [Fact]
    public void PublishRejectsArticleThatWasNotApproved()
    {
        var article = NewsArticle.Create(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Mutirão de serviços municipais",
            "mutirao-de-servicos-municipais",
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        var error = Assert.Throws<EditorialTransitionException>(() =>
            article.Publish(Guid.NewGuid(), DateTimeOffset.UtcNow));

        Assert.Contains("APPROVED", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ApproveAndPublishRecordsPublicationWithoutLosingHistoryVersion()
    {
        var article = NewsArticle.Create(Guid.NewGuid(), "Título", "titulo", Guid.NewGuid());
        article.SubmitForReview(Guid.NewGuid(), DateTimeOffset.UtcNow);
        article.Approve(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var publisher = Guid.NewGuid();
        var publishedAt = new DateTimeOffset(2026, 8, 25, 14, 30, 0, TimeSpan.Zero);
        article.Publish(publisher, publishedAt);

        Assert.Equal(EditorialStatus.Published, article.Status);
        Assert.Equal(publishedAt, article.PublishedAt);
        Assert.Equal(publisher, article.PublishedBy);
        Assert.Equal(3, article.Version);
    }
}
