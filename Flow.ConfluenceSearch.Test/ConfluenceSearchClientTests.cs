using System.Linq;
using System.Net;
using System.Text.Json;
using Flow.ConfluenceSearch.ConfluenceClient;
using Shouldly;

namespace Flow.ConfluenceSearch.Test;

public class ConfluenceSearchClientTests : IDisposable
{
    private readonly TestHttpMessageHandler _httpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly ConfluenceSearchClient _searchClient;

    public ConfluenceSearchClientTests()
    {
        _httpMessageHandler = new TestHttpMessageHandler();
        _httpClient = new HttpClient(_httpMessageHandler)
        {
            BaseAddress = new Uri("https://test.atlassian.net/"),
        };
        var httpFactory = () => _httpClient;
        _searchClient = new ConfluenceSearchClient(httpFactory);
    }

    [Fact]
    public async Task SearchCqlAsync_WithValidCql_ReturnsConfluenceResponse()
    {
        // Arrange
        const string cql = "project = TEST";
        const int maxResults = 50;
        var cancellationToken = CancellationToken.None;

        var expectedResponse = new ContentSearchResponse
        {
            Results = new List<ConfluenceSearchItemDto>
            {
                new()
                {
                    Excerpt = "TEST-1",
                    Content = new ConfluenceContentDto
                    {
                        Id = "Test Issue Summary",
                        History = new ConfluenceHistoryDto
                        {
                            LastUpdated = new ConfluenceLastUpdatedDto
                            {
                                By = new ConfluenceUserDto
                                {
                                    DisplayName = "John Doe",
                                    ProfilePicture = new ConfluenceProfilePictureDto
                                    {
                                        Path = "High",
                                    },
                                },
                            },
                        },
                    },
                    Title = "TEST",
                    Url = "Bug",
                    ResultGlobalContainer = new ResultGlobalContainerDto
                    {
                        DisplayUrl = "/spaces/John Doe/pages",
                    },
                },
            },
        };
        _httpMessageHandler.SetResponse(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedResponse)
        );

        // Act
        var result = await _searchClient.SearchCqlAsync(cql, maxResults, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Results.ShouldNotBeEmpty();
        result.Results.Count.ShouldBe(1);

        // Test issue details
        var issue = result.Results[0];
        issue.Excerpt.ShouldBe("TEST-1");
        issue.Content.Id.ShouldBe("Test Issue Summary");
        issue.LastUpdatedByAvatarPath.ShouldBe("High");
        issue.SpaceKey.ShouldBe("John Doe");
        issue.Title.ShouldBe("TEST");
        issue.Url.ShouldBe("Bug");
    }

    [Fact]
    public async Task SearchCqlAsync_WhenApiReturnsError_ThrowsApplicationException()
    {
        // Arrange
        var errorContent = "server error details";
        _httpMessageHandler.SetResponse(HttpStatusCode.InternalServerError, errorContent);

        // Act & Assert
        var ex = await Should.ThrowAsync<ApplicationException>(async () =>
            await _searchClient.SearchCqlAsync("invalid cql", 10, CancellationToken.None)
        );
        ex.Message.ShouldContain(errorContent);
    }

    [Fact]
    public async Task SearchCqlAsync_WithEmptyResults_ReturnsEmptyList()
    {
        // Arrange
        var expected = new ContentSearchResponse { Results = new List<ConfluenceSearchItemDto>() };
        _httpMessageHandler.SetResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expected));

        // Act
        var result = await _searchClient.SearchCqlAsync("no results", 10, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Results.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchCqlAsync_WithSpecialCharacters_PreservesFieldsAndBuildsBrowseUrl()
    {
        // Arrange
        var special = "\\*+-!():^[]{}~?|&/\\\"'";
        var expected = new ContentSearchResponse
        {
            Results = new List<ConfluenceSearchItemDto>
            {
                new()
                {
                    Excerpt = special,
                    Title = special,
                    Url = "/pages/viewpage.action?pageId=123",
                    Content = new ConfluenceContentDto
                    {
                        Id = special,
                        History = new ConfluenceHistoryDto
                        {
                            LastUpdated = new ConfluenceLastUpdatedDto
                            {
                                By = new ConfluenceUserDto
                                {
                                    DisplayName = special,
                                    ProfilePicture = new ConfluenceProfilePictureDto
                                    {
                                        Path = "/avatars/1",
                                    },
                                },
                            },
                        },
                    },
                    ResultGlobalContainer = new ResultGlobalContainerDto
                    {
                        DisplayUrl = "/spaces/KEY/pages",
                    },
                },
            },
        };
        _httpMessageHandler.SetResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expected));

        // Act
        var result = await _searchClient.SearchCqlAsync("special", 10, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Results.Count.ShouldBe(1);
        var item = result.Results.Single();
        item.Title.ShouldBe(special);
        item.Excerpt.ShouldBe(special);
        item.Content.Id.ShouldBe(special);
        item.LastUpdatedByName.ShouldBe(special);
        item.LastUpdatedByAvatarPath.ShouldBe("/avatars/1");

        // BrowseUrl should contain base + /wiki + url
        var browse = item.BrowseUrl(_httpClient.BaseAddress!.ToString());
        browse.ShouldContain("/wiki");
        browse.ShouldContain("viewpage.action");
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _httpMessageHandler?.Dispose();
        GC.SuppressFinalize(this);
    }
}
