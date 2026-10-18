# Friday release security simulation

A small, deliberately flawed experiment behind the post _A Friday Release, a
Tenant Boundary, and One Security Alert_ on [raghib.io](https://raghib.io).

> **Disclaimer:** I work at Microsoft. This is my personal view, based only on
> public information. It isn't an official position or a product
> recommendation.

## The claim

A green check answers a narrow question: the configured check passed. It does
not prove that a feature is secure.

This repository tests that claim with one feature: invoice search in a
multi-tenant API. Each tenant must see only its own invoices. Two separate
protections matter:

- **Query parameters** stop input from changing the SQL.
- **A tenant filter** decides who may read a row.

Fixing one does not fix the other.

## The experiment

`main` is the baseline. Its `GET /invoices` endpoint is already scoped to the
caller's tenant, and it has tests for that. Three pull requests then add
`GET /invoices/search?number=…`:

| PR | Change | Expected tests | Expected CodeQL | Tenant A can read tenant B's invoice? |
| --- | --- | --- | --- | --- |
| 1 | Rushed: SQL built from input, no tenant filter | Pass | SQL injection alert | Yes |
| 2 | Parameterized query, still no tenant filter | Pass | No alert | Yes |
| 3 | Boundary tests first, then the tenant filter | Fail, then pass | No alert | No |

The pull requests stay open and unmerged, so their check runs remain visible.
The results section records what actually happened, including any misses.

## Results

Recorded on 17 October 2026 with CodeQL 2.27.2 (default query suite,
`build-mode: none`) and the workflows in `.github/workflows`.

| Run | Tests | CodeQL check | Tenant A reads tenant B's `INV-2001`? |
| --- | --- | --- | --- |
| `main` | [Pass, 2 of 2][main-ci] | [Pass, 0 alerts][main-codeql] | Not applicable, no search |
| [#1][pr1] rushed | [Pass, 3 of 3][pr1-ci] | [Fail: 1 high `cs/sql-injection` alert][pr1-codeql] | Yes |
| [#2][pr2] parameterized | [Pass, 3 of 3][pr2-ci] | [Pass, 0 alerts][pr2-codeql] | Yes |
| [#3][pr3] commit 1, tests only | [Fail: cross-tenant test][pr3a-ci] | [Pass, 0 alerts][pr3a-codeql] | Yes |
| [#3][pr3] commit 2, tenant filter | [Pass, 5 of 5][pr3b-ci] | [Pass, 0 alerts][pr3b-codeql] | No |

The CodeQL alert on #1 pointed at the interpolated query in
`InvoiceStore.cs`, with the message "This query depends on this ASP.NET Core
MVC action method parameter."

Running #1 locally showed both risks:

```text
$ curl -H "X-Api-Key: demo-key-tenant-a" ".../invoices/search?number=INV-2001"
[{"number":"INV-2001","billedTo":"Birch Studio","total":4300.00}]

$ curl -H "X-Api-Key: demo-key-tenant-a" ".../invoices/search?number=' OR '1'='1"
[...all four invoices, from both tenants...]
```

Locally, the two boundary tests from #3 both fail against #1's code, only the
cross-tenant test fails against #2's code, and both pass against #3's fix.

### What this shows

- The scanner caught the injection in #1. The tests did not.
- Nothing caught the missing tenant filter until a test stated the rule. #2
  passed every check and still returned another tenant's invoice.
- That is not a scanner bug. The default queries do not know this app's tenant
  rule. It is the limit of what a green check means.

[main-ci]: https://github.com/ragmha/friday-release-security-simulation/actions/runs/39101433908
[main-codeql]: https://github.com/ragmha/friday-release-security-simulation/actions/runs/39101433911
[pr1]: https://github.com/ragmha/friday-release-security-simulation/pull/1
[pr1-ci]: https://github.com/ragmha/friday-release-security-simulation/actions/runs/39101579523
[pr1-codeql]: https://github.com/ragmha/friday-release-security-simulation/runs/117599021302
[pr2]: https://github.com/ragmha/friday-release-security-simulation/pull/2
[pr2-ci]: https://github.com/ragmha/friday-release-security-simulation/actions/runs/39101581730
[pr2-codeql]: https://github.com/ragmha/friday-release-security-simulation/runs/117599047347
[pr3]: https://github.com/ragmha/friday-release-security-simulation/pull/3
[pr3a-ci]: https://github.com/ragmha/friday-release-security-simulation/actions/runs/39101583351
[pr3a-codeql]: https://github.com/ragmha/friday-release-security-simulation/actions/runs/39101583413
[pr3b-ci]: https://github.com/ragmha/friday-release-security-simulation/actions/runs/39102030345
[pr3b-codeql]: https://github.com/ragmha/friday-release-security-simulation/runs/117600279824

## Run it locally

Requires the .NET 10 SDK.

```sh
dotnet test
dotnet run --project src/InvoiceApi
curl -H "X-Api-Key: demo-key-tenant-a" http://localhost:5244/invoices
```

## Boundaries

- Synthetic data only, in an in-memory SQLite database. The demo API keys are
  not secrets.
- The vulnerable code lives only on the pull request branches. Don't deploy it.
- One feature and one scanner configuration. This shows how one change behaved,
  not how often any tool finds vulnerabilities.
