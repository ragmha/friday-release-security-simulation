using System.Net;
using System.Net.Http.Json;
using InvoiceApi.Data;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InvoiceApi.Tests;

public sealed class InvoiceApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TenantA = "demo-key-tenant-a";

    [Fact]
    public async Task Listing_requires_an_api_key()
    {
        var response = await factory.CreateClient().GetAsync("/invoices");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Listing_returns_only_the_callers_invoices()
    {
        var invoices = await GetInvoices(TenantA, "/invoices");

        Assert.Equal(["INV-1001", "INV-1002"], invoices.Select(invoice => invoice.Number));
    }

    [Fact]
    public async Task Search_finds_an_invoice_by_number()
    {
        var invoices = await GetInvoices(TenantA, "/invoices/search?number=INV-1001");

        Assert.Equal("INV-1001", Assert.Single(invoices).Number);
    }

    private async Task<Invoice[]> GetInvoices(string apiKey, string path)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        return await client.GetFromJsonAsync<Invoice[]>(path) ?? [];
    }
}
