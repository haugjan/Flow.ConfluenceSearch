# Query syntax

The `conf` action keyword takes a free-form query made of space-separated
tokens. `ConfluenceQueryBuilder` parses the tokens (in any order) and
produces:

1. A **CQL query** for the Confluence REST search endpoint.
2. A **URL parameter string** used as a fallback for "open this search in
   the browser" when the API call returns nothing or the user wants more
   results.

## Tokens

| Token         | Effect on CQL                                                     |
|---------------|-------------------------------------------------------------------|
| `#all`        | Disables the default-spaces filter (no `space IN (...)` clause).  |
| `#KEY`        | Restricts to space `KEY`. Repeatable (`#A #B` → `space IN (A,B)`). |
| `@me`         | `contributor = currentUser()`.                                    |
| `@name`       | `contributor.fullname ~ name`. Repeatable. Combined with `@me` via `OR`. |
| `+label`      | `label IN (label)`. Repeatable.                                   |
| `/`           | `type IN (folder)`.                                               |
| `"`           | `type IN (blogpost)`.                                             |
| `.`           | `type IN (page)`.                                                 |
| `*`           | Skip the type filter entirely (default is `type IN(page,blogpost)`). |
| any other     | Free-text term → `(title ~ "tok*" OR text ~ "tok*")`.             |

The CQL output is always suffixed with `order by lastmodified DESC`.

If no `#KEY` and no `#all` are given, the spaces from `Default Spaces` in
the plugin settings are applied.

## Examples

```
conf project plan
→ space IN (DEFAULTS) AND type IN(page,blogpost)
   AND (title~"project* plan*" OR text~"project* plan*")
   order by lastmodified DESC

conf @me #docs
→ space IN (docs) AND type IN(page,blogpost)
   AND (contributor = currentUser())
   order by lastmodified DESC

conf #all meeting notes
→ type IN(page,blogpost)
   AND (title~"meeting* notes*" OR text~"meeting* notes*")
   order by lastmodified DESC

conf @john +retro /
→ space IN (DEFAULTS) AND type IN(folder) AND label IN (retro)
   AND (contributor.fullname ~ john)
   order by lastmodified DESC
```

The full list of expected CQL outputs is encoded as `[InlineData]` rows on
`Flow.ConfluenceSearch.Test/ConfluenceQueryBuilderTest.cs`. Treat that file
as the canonical specification.

## Internals

The builder is implemented as a small fluent pipeline in
`QueryBuilderExtensions.cs`. Each `When(regex)` step matches whole tokens,
optionally captures groups, and feeds them into `Then…` actions:

- `Then(part)` — append a literal CQL fragment.
- `ThenRemember(value)` — push a literal value into a memory buffer.
- `ThenRemember()` — push the captured group(s) into the buffer.
- `ThenRemember(convert)` — async-transform captures before pushing.
- `ThenDoNothing()` — match-and-consume without side effects (used for
  `*` and `#all`).
- `Aggregate(fn)` — fold the buffer into one CQL part and clear it.
- `Else(part)` — fallback CQL part if no match in the preceding `When` chain.
- `BuildCql(separator)` — join all collected parts with `AND` (CQL) or
  `&` (URL parameters).

Free-text escaping for the CQL `~` operator happens in
`ConfluenceQueryBuilder.EscapeForCqlToken`: each token is `Trim`med, the
trailing `*` is removed, the special characters
`\ * + - ! ( ) : ^ [ ] { } ~ ? | & / " '` are backslash-escaped, and a
single trailing `*` is appended for prefix matching.
