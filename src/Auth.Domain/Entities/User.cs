using Auth.Domain.Common.Entities;

namespace Auth.Domain.Entities;

public sealed class User : AuditableEntity
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string Email { get; set; } = default!;
    public bool IsActive { get; set; }
}