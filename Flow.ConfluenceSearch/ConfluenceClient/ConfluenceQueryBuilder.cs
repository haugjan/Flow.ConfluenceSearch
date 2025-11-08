namespace Flow.ConfluenceSearch.ConfluenceClient;

internal interface IConfluenceQueryBuilder
{
    Task<string> BuildTextCql(
        string text,
        IReadOnlyList<string> spaces,
        CancellationToken cancellationToken
    );

    Task<string> BuildQueryForOpenInBrowser(
        string text,
        IReadOnlyList<string> spaces,
        CancellationToken cancellationToken
    );
}

internal class ConfluenceQueryBuilder : IConfluenceQueryBuilder
{
    public async Task<string> BuildTextCql(
        string text,
        IReadOnlyList<string> spaces,
        CancellationToken cancellationToken
    )
    {
        var tokens = text.Tokenize();

        return await tokens
                .When("#all")
                .ThenDoNothing()
                .When("#([a-zA-Z]{2,})")
                .ThenRemember()
                .Aggregate(mem => $"space IN ({string.Join(",", mem)})")
                .Else(spaces.Count > 0 ? $"space IN ({string.Join(",", spaces)})" : string.Empty)
                .When(@"\*")
                .ThenDoNothing()
                .When(@"\/")
                .ThenRemember("folder")
                .When("\"")
                .ThenRemember("blogpost")
                .When(".")
                .ThenRemember("page")
                .Aggregate(mem => $"type IN({string.Join(",", mem)})")
                .Else("type IN(page,blogpost)")
                .When(@"\+([a-zA-Z0-9]{2,})")
                .ThenRemember()
                .Aggregate(mem => $"label IN ({string.Join(",", mem)})")
                .When("@me")
                .ThenRemember("contributor = currentUser()")
                .When(@"@([\p{L}-]{2,})")
                .ThenRemember(input => Task.FromResult<IEnumerable<string>>([$"contributor.fullname ~ {input}"]))
                .Aggregate(mem => $"({string.Join(" OR ", mem)})")
                .When(".*")
                .ThenRemember()
                .Aggregate(CreateSearchCql)
                .BuildCql(" AND ") + " order by lastmodified DESC";
    }

    public async Task<string> BuildQueryForOpenInBrowser(
        string text,
        IReadOnlyList<string> spaces,
        CancellationToken cancellationToken
    )
    {
        var tokens = text.Tokenize();

        return await tokens
            .When("#all")
            .ThenDoNothing()
            .When("#([a-zA-Z]{2,})")
            .ThenRemember()
            .Aggregate(mem => $"space={string.Join(",", mem)}")
            .Else(spaces.Count > 0 ? $"spaces={string.Join(",", spaces)}" : string.Empty)
            .When(@"\*")
            .ThenDoNothing()
            .When(@"\/")
            .Then("type=folder")
            .When("\"")
            .Then("type=blogpost")
            .When(".")
            .Then("type=page")
            .When(@"\+([a-zA-Z0-9]{2,})")
            .ThenRemember()
            .Aggregate(mem => $"labels={string.Join(",", mem)}")
            .When("@me")
            .ThenDoNothing()
            .When(@"@([\p{L}-]{2,})")
            .ThenDoNothing()
            .When(".*")
            .ThenRemember()
            .Aggregate(mem => $"text={string.Join(" ", mem)}")
            .BuildCql("&");
    }

    private static string EscapeForCqlToken(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return string.Empty;
        s = s.Trim().TrimEnd('*');

        var specialChars = new HashSet<char>("\\*+-!():^[]{}~?|&/\"'");
        var sb = new System.Text.StringBuilder();

        foreach (var c in s)
        {
            if (specialChars.Contains(c))
                sb.Append('\\');
            sb.Append(c);
        }

        sb.Append('*');
        return sb.ToString();
    }

    private string CreateSearchCql(IEnumerable<string> mem)
    {
        var tokens = mem.Select(EscapeForCqlToken).Where(t => !string.IsNullOrEmpty(t)).ToArray();

        if (tokens.Length == 0)
            return "(title~\"\" OR text~\"\")";

        var searchText = string.Join(" ", tokens);

        return $"(title~\"{searchText}\" OR text~\"{searchText}\")";
    }
}
