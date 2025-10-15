using System;
using System.Collections.Generic;

namespace Repositories.Model;

public partial class User
{
    public int Id { get; set; }

    public int RoleId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Dealer? Dealer { get; set; }

    public virtual Role Role { get; set; } = null!;
}
