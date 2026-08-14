# Contributing to YTLiveChat

Thanks for your interest. Bug reports, reproductions, and small focused pull requests are all
welcome. Please open an issue before starting anything large, so we can agree on the shape before
you spend time on it.

## Getting set up

You need the .NET SDK pinned in [`global.json`](global.json). `dotnet` will tell you if the
installed SDK does not match.

```bash
dotnet restore YTLiveChat.sln
dotnet build YTLiveChat.sln
dotnet test YTLiveChat.sln
```

Live-network tests are marked `[TestCategory("Integration")]` and are excluded from the default
gate. Run everything except those the way CI does:

```bash
dotnet test YTLiveChat.sln --filter "TestCategory!=Integration"
```

## House rules

- **Warnings are errors.** `TreatWarningsAsErrors` is on. Fix the diagnostic rather than suppressing
  it; a `NoWarn` or `#pragma` needs a comment saying why the rule genuinely does not apply.
- **Nullable reference types are enabled** everywhere. No `!` without a reason.
- **All I/O is async**, with a `CancellationToken` accepted and propagated. No `.Result`,
  `.GetAwaiter().GetResult()`, or `Thread.Sleep`.
- **Public API carries XML documentation.** The build generates a documentation file and will fail
  on missing comments.
- **The package is trim- and AOT-clean.** `IsAotCompatible` is set, so the trim and AOT analyzers
  run on every build. Serialization goes through the source-generated `JsonSerializerContext`, never
  the reflection-based `JsonSerializer` overloads.

## Tests

- Name tests `{Method}_{Scenario}_{ExpectedResult}`.
- Prefer the purpose-built MSTest assertions (`Assert.HasCount`, `Assert.Contains`,
  `Assert.AreSequenceEqual`) over hand-rolled equality checks — the analyzers will point you at them.
- No `Thread.Sleep`. Use `TaskCompletionSource`, channels, or a fake clock.
- New behaviour needs a test. Bug fixes need a test that fails before the fix.

## Commits and pull requests

Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/):

```
fix(webhooks): reject a signature computed over the decoded body
```

Keep the subject under 50 characters and in the imperative mood. Add a body only when the reason
for the change would not be obvious to the next reader — explain *why*, not *what*.

One logical change per commit. Rebase rather than merge when updating a branch.

## Reporting security issues

Please do not open a public issue. See [SECURITY.md](SECURITY.md).
