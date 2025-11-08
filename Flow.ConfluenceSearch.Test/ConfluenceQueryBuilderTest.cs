using Flow.ConfluenceSearch.ConfluenceClient;
using Shouldly;

namespace Flow.ConfluenceSearch.Test;

public class ConfluenceQueryBuilderTest
{
    [Theory]
    [InlineData(
        "test",
        "space IN (AAA,BBB) AND type IN(page,blogpost) AND (title~\"test*\" OR text~\"test*\") order by lastmodified DESC"
    )]
    [InlineData(
        "test*",
        "space IN (AAA,BBB) AND type IN(page,blogpost) AND (title~\"test*\" OR text~\"test*\") order by lastmodified DESC"
    )]
    [InlineData(
        "test example",
        "space IN (AAA,BBB) AND type IN(page,blogpost) AND (title~\"test* example*\" OR text~\"test* example*\") order by lastmodified DESC"
    )]
    [InlineData(
        "test* example*",
        "space IN (AAA,BBB) AND type IN(page,blogpost) AND (title~\"test* example*\" OR text~\"test* example*\") order by lastmodified DESC"
    )]
    [InlineData(
        "*test",
        "space IN (AAA,BBB) AND type IN(page,blogpost) AND (title~\"\\*test*\" OR text~\"\\*test*\") order by lastmodified DESC"
    )]
    [InlineData(
        "\\*+-!():^[]{}~?|&/\"'",
        "space IN (AAA,BBB) AND type IN(page,blogpost) AND (title~\"\\\\\\*\\+\\-\\!\\(\\)\\:\\^\\[\\]\\{\\}\\~\\?\\|\\&\\/\\\"\\\'*\" OR text~\"\\\\\\*\\+\\-\\!\\(\\)\\:\\^\\[\\]\\{\\}\\~\\?\\|\\&\\/\\\"\\\'*\") order by lastmodified DESC"
    )]
    [InlineData(
        "test #myspace",
        "space IN (myspace) AND type IN(page,blogpost) AND (title~\"test*\" OR text~\"test*\") order by lastmodified DESC"
    )]
    [InlineData(
        "test #myspace #yourspace",
        "space IN (myspace,yourspace) AND type IN(page,blogpost) AND (title~\"test*\" OR text~\"test*\") order by lastmodified DESC"
    )]
    [InlineData(
        "test @me",
        "space IN (AAA,BBB) AND type IN(page,blogpost) AND (contributor = currentUser()) AND (title~\"test*\" OR text~\"test*\") order by lastmodified DESC"
    )]
    [InlineData(
        "test @john",
        "space IN (AAA,BBB) AND type IN(page,blogpost) AND (contributor.fullname ~ john) AND (title~\"test*\" OR text~\"test*\") order by lastmodified DESC"
    )]
    [InlineData(
        "test @john @me",
        "space IN (AAA,BBB) AND type IN(page,blogpost) AND (contributor = currentUser() OR contributor.fullname ~ john) AND (title~\"test*\" OR text~\"test*\") order by lastmodified DESC"
    )]
    [InlineData(
        "test +label1",
        "space IN (AAA,BBB) AND type IN(page,blogpost) AND label IN (label1) AND (title~\"test*\" OR text~\"test*\") order by lastmodified DESC"
    )]
    [InlineData(
        "test +label1 +label2",
        "space IN (AAA,BBB) AND type IN(page,blogpost) AND label IN (label1,label2) AND (title~\"test*\" OR text~\"test*\") order by lastmodified DESC"
    )]
    [InlineData(
        "test /",
        "space IN (AAA,BBB) AND type IN(folder) AND (title~\"test*\" OR text~\"test*\") order by lastmodified DESC"
    )]
    [InlineData(
        "test .",
        "space IN (AAA,BBB) AND type IN(page) AND (title~\"test*\" OR text~\"test*\") order by lastmodified DESC"
    )]
    [InlineData(
        "test \"",
        "space IN (AAA,BBB) AND type IN(blogpost) AND (title~\"test*\" OR text~\"test*\") order by lastmodified DESC"
    )]
    [InlineData(
        "test / . \"",
        "space IN (AAA,BBB) AND type IN(folder,blogpost,page) AND (title~\"test*\" OR text~\"test*\") order by lastmodified DESC"
    )]
    [InlineData(
        "test *",
        "space IN (AAA,BBB) AND (title~\"test*\" OR text~\"test*\") order by lastmodified DESC"
    )]
    [InlineData(
        "test / +label1 @me #myspace @john . +label2 \" #yourspace",
        "space IN (myspace,yourspace) AND type IN(folder,blogpost,page) AND label IN (label1,label2) AND (contributor = currentUser() OR contributor.fullname ~ john) AND (title~\"test*\" OR text~\"test*\") order by lastmodified DESC"
    )]
    public async Task BuildTextCqlTest(string input, string expectedOutput)
    {
        // Act
        var queryBuilder = new ConfluenceQueryBuilder();

        var output = await queryBuilder.BuildTextCql(
            input,
            ["AAA", "BBB"],
            TestContext.Current.CancellationToken
        );

        output.ShouldBe(expectedOutput);
    }

    [Theory]
    [InlineData("test", "spaces=AAA,BBB&text=test")]
    [InlineData("test*", "spaces=AAA,BBB&text=test*")]
    [InlineData("test example", "spaces=AAA,BBB&text=test example")]
    [InlineData("test* example*", "spaces=AAA,BBB&text=test* example*")]
    [InlineData("*test", "spaces=AAA,BBB&text=*test")]
    [InlineData(
        "spaces=AAA,BBB&text=\\*+-!():^[]{}~?|&/\"'",
        "spaces=AAA,BBB&text=spaces=AAA,BBB&text=\\*+-!():^[]{}~?|&/\"'"
    )]
    [InlineData("test #myspace", "space=myspace&text=test")]
    [InlineData("test #myspace #yourspace", "space=myspace,yourspace&text=test")]
    [InlineData("test @me", "spaces=AAA,BBB&text=test")]
    [InlineData("test @john", "spaces=AAA,BBB&text=test")]
    [InlineData("test @john @me", "spaces=AAA,BBB&text=test")]
    [InlineData("test +label1", "spaces=AAA,BBB&labels=label1&text=test")]
    [InlineData("test +label1 +label2", "spaces=AAA,BBB&labels=label1,label2&text=test")]
    [InlineData("test /", "spaces=AAA,BBB&type=folder&text=test")]
    [InlineData("test .", "spaces=AAA,BBB&type=page&text=test")]
    [InlineData("test \"", "spaces=AAA,BBB&type=blogpost&text=test")]
    [InlineData("test / . \"", "spaces=AAA,BBB&type=folder&type=blogpost&type=page&text=test")]
    [InlineData("test *", "spaces=AAA,BBB&text=test")]
    [InlineData(
        "test / +label1 @me #myspace @john . +label2 : #yourspace",
        "space=myspace,yourspace&type=folder&type=page&labels=label1,label2&text=test"
    )]
    public async Task BuildQueryForOpenInBrowserTest(string input, string expectedOutput)
    {
        // Act
        var queryBuilder = new ConfluenceQueryBuilder();

        var output = await queryBuilder.BuildQueryForOpenInBrowser(
            input,
            ["AAA", "BBB"],
            TestContext.Current.CancellationToken
        );

        output.ShouldBe(expectedOutput);
    }
}
