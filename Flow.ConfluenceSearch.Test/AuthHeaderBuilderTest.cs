using System.Text;
using Flow.ConfluenceSearch.Settings;
using Shouldly;

namespace Flow.ConfluenceSearch.Test;

public class AuthHeaderBuilderTest
{
    [Fact]
    public void EmailAndToken_AreCombinedAsBasicCredential()
    {
        var header = AuthHeaderBuilder.Build("user@example.com", "secret-token");

        Decode(header).ShouldBe("user@example.com:secret-token");
    }

    [Fact]
    public void EmptyEmail_FallsBackToTokenAsIs()
    {
        var header = AuthHeaderBuilder.Build("", "personal-access-token");

        Decode(header).ShouldBe("personal-access-token");
    }

    [Fact]
    public void EmptyEmail_PreservesLegacyEmailColonTokenInTokenField()
    {
        var header = AuthHeaderBuilder.Build("", "user@example.com:secret-token");

        Decode(header).ShouldBe("user@example.com:secret-token");
    }

    [Fact]
    public void NullInputs_ProduceEmptyCredential()
    {
        var header = AuthHeaderBuilder.Build(null, null);

        Decode(header).ShouldBe(string.Empty);
    }

    [Fact]
    public void Whitespace_IsTrimmedFromBothFields()
    {
        var header = AuthHeaderBuilder.Build("  user@example.com  ", "  secret-token  ");

        Decode(header).ShouldBe("user@example.com:secret-token");
    }

    private static string Decode(string base64) =>
        Encoding.UTF8.GetString(Convert.FromBase64String(base64));
}
