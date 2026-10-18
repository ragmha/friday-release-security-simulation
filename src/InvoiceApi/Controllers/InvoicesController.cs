using InvoiceApi.Auth;
using InvoiceApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceApi.Controllers;

[ApiController]
[Authorize]
[Route("invoices")]
public sealed class InvoicesController(InvoiceStore store) : ControllerBase
{
    [HttpGet]
    public IReadOnlyList<Invoice> List() => store.ListForTenant(User.TenantId());
}
