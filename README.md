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

Pending the first run.

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
