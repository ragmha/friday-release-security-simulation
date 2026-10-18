using Microsoft.Data.Sqlite;

namespace InvoiceApi.Data;

public sealed record Invoice(string Number, string BilledTo, decimal Total);

// Synthetic data in a private in-memory SQLite database. Nothing here is real.
public sealed class InvoiceStore : IDisposable
{
    private const string SeedSql = """
        CREATE TABLE ApiKeys (Key TEXT PRIMARY KEY, TenantId TEXT NOT NULL);
        CREATE TABLE Invoices (
            Number TEXT PRIMARY KEY,
            TenantId TEXT NOT NULL,
            BilledTo TEXT NOT NULL,
            Total TEXT NOT NULL);

        INSERT INTO ApiKeys VALUES
            ('demo-key-tenant-a', 'aaaaaaaa-0000-0000-0000-000000000001'),
            ('demo-key-tenant-b', 'bbbbbbbb-0000-0000-0000-000000000002');

        INSERT INTO Invoices VALUES
            ('INV-1001', 'aaaaaaaa-0000-0000-0000-000000000001', 'Acme Bakery', '120.00'),
            ('INV-1002', 'aaaaaaaa-0000-0000-0000-000000000001', 'Acme Bakery', '89.50'),
            ('INV-2001', 'bbbbbbbb-0000-0000-0000-000000000002', 'Birch Studio', '4300.00'),
            ('INV-2002', 'bbbbbbbb-0000-0000-0000-000000000002', 'Birch Studio', '615.25');
        """;

    private readonly string _connectionString =
        $"Data Source=invoices-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

    // An in-memory database lives only while at least one connection is open.
    private readonly SqliteConnection _keepAlive;

    public InvoiceStore()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
        using var command = _keepAlive.CreateCommand();
        command.CommandText = SeedSql;
        command.ExecuteNonQuery();
    }

    public Guid? FindTenantByApiKey(string apiKey)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT TenantId FROM ApiKeys WHERE Key = $key";
        command.Parameters.AddWithValue("$key", apiKey);
        return command.ExecuteScalar() is string tenantId ? Guid.Parse(tenantId) : null;
    }

    public IReadOnlyList<Invoice> ListForTenant(Guid tenantId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT Number, BilledTo, Total FROM Invoices WHERE TenantId = $tenantId ORDER BY Number";
        command.Parameters.AddWithValue("$tenantId", tenantId.ToString());
        return Read(command);
    }

    public IReadOnlyList<Invoice> FindByNumber(string number)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT Number, BilledTo, Total FROM Invoices WHERE Number = $number";
        command.Parameters.AddWithValue("$number", number);
        return Read(command);
    }

    public void Dispose() => _keepAlive.Dispose();

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static List<Invoice> Read(SqliteCommand command)
    {
        using var reader = command.ExecuteReader();
        var invoices = new List<Invoice>();
        while (reader.Read())
        {
            invoices.Add(new Invoice(reader.GetString(0), reader.GetString(1), reader.GetDecimal(2)));
        }
        return invoices;
    }
}
