using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Flow.ConfluenceSearch.ConfluenceClient;

public static class QueryBuilderExtensions
{
    public record Query(
        IImmutableList<string> Tokens,
        IImmutableList<string> QueryParts,
        IImmutableList<string> Captures,
        IImmutableList<string> Memory,
        bool HadMatches = false
    );

    // Start der Fluent API
    public static Task<Query> Tokenize(this string input)
    {
        var textTokens = input
            .Split([' '], StringSplitOptions.RemoveEmptyEntries)
            .ToImmutableList();
        return Task.FromResult(
            new Query(
                Tokens: textTokens,
                QueryParts: ImmutableList<string>.Empty,
                Captures: ImmutableList<string>.Empty,
                Memory: ImmutableList<string>.Empty,
                HadMatches: false
            )
        );
    }

    public static async Task<Query> When(this Task<Query> queryTask, string regexPattern)
    {
        var query = await queryTask;
        var regex = new Regex(
            $"^{regexPattern}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant
        );
        var matches = query
            .Tokens.Select(token => regex.Match(token))
            .Where(match => match.Success)
            .ToList();

        var tokens = query.Tokens.RemoveAll(token => matches.Any(m => m.Value == token));
        var captures = matches
            .Select(match => match.Groups.Count > 1 ? match.Groups[1].Value : match.Value) // Fix: Groups[1] für erste Capture-Gruppe
            .Distinct()
            .ToImmutableList();

        return query with
        {
            Tokens = tokens,
            Captures = captures,
        };
    }

    // ThenRemember-Methoden
    public static async Task<Query> ThenRemember(this Task<Query> queryTask, string tokenToRemember)
    {
        var query = await queryTask;
        if (!query.Captures.Any())
            return query;

        return query with
        {
            Captures = ImmutableList<string>.Empty,
            Memory = query.Memory.Add(tokenToRemember),
        };
    }

    public static async Task<Query> ThenRemember(this Task<Query> queryTask)
    {
        var query = await queryTask;
        return query with
        {
            Captures = ImmutableList<string>.Empty,
            Memory = query.Memory.AddRange(query.Captures),
        };
    }

    public static async Task<Query> ThenRemember(
        this Task<Query> queryTask,
        Func<string, Task<IEnumerable<string>>> convertCapture
    )
    {
        var query = await queryTask;
        if (!query.Captures.Any())
            return query;

        var convertedTokens = await Task.WhenAll(query.Captures.Select(convertCapture));
        var tokens = convertedTokens
            .SelectMany(x => x)
            .Distinct()
            .ToImmutableList();
        return query with
        {
            Captures = ImmutableList<string>.Empty,
            Memory = query.Memory.AddRange(tokens),
        };
    }

    // Then-Methoden
    public static async Task<Query> Then(this Task<Query> queryTask, string cqlPart)
    {
        var query = await queryTask;
        if (!query.Captures.Any())
            return query;

        var parts = query.QueryParts.Add(cqlPart);
        return query with { QueryParts = parts, HadMatches = true };
    }

    public static async Task<Query> ThenDoNothing(this Task<Query> queryTask)
    {
        var query = await queryTask;
        if (!query.Captures.Any())
            return query;

        return query with
        {
            HadMatches = true,
            Captures = ImmutableList<string>.Empty,
        };
    }

    // Else-Methode
    public static async Task<Query> Else(this Task<Query> queryTask, string cqlPart)
    {
        var query = await queryTask;
        if (query.HadMatches)
            return query with { HadMatches = false };

        return query with
        {
            HadMatches = false,
            QueryParts = query.QueryParts.Add(cqlPart),
        };
    }

    public static async Task<Query> Aggregate(
        this Task<Query> queryTask,
        Func<IEnumerable<string>, string> aggregator
    )
    {
        var query = await queryTask;
        if (!query.Memory.Any())
            return query with { Captures = ImmutableList<string>.Empty };

        var aggregated = aggregator(query.Memory);
        return query with
        {
            QueryParts = query.QueryParts.Add(aggregated),
            Memory = ImmutableList<string>.Empty,
            HadMatches = true,
        };
    }

    public static async Task<string> BuildCql(this Task<Query> queryTask, string separator)
    {
        var query = await queryTask;
        return string.Join(
            separator,
            query.QueryParts.Where(part => !string.IsNullOrWhiteSpace(part))
        );
    }
}
