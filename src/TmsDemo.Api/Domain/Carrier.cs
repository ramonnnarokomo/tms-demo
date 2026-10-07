namespace TmsDemo.Api.Domain;

/// <summary>A transport company that can be assigned to shipments.</summary>
public sealed class Carrier
{
    // Used by EF Core when loading entities from the database.
    private Carrier()
    {
    }

    public Carrier(string name, string? email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Carrier name is required.");

        Name = name.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        IsActive = true;
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public bool IsActive { get; private set; }

    public void Deactivate() => IsActive = false;

    /// <summary>Lets the carrier take new shipments again (e.g. after renewing its contract).</summary>
    public void Activate() => IsActive = true;
}
