using System;
using System.Collections.Generic;

namespace app_salvamentos.Models;

public partial class Usuario
{
    public int UsuarioId { get; set; }

    public string UsuarioLogin { get; set; } = null!;

    public string UsuarioEmail { get; set; } = null!;

    public string UsuarioPasswordHash { get; set; } = null!;

    public int PerfilId { get; set; }

    public bool UsuarioEstado { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public Guid PasswordSalt { get; set; }

    public int FailedLoginCount { get; set; }

    public DateTime? LockoutUntil { get; set; }

    public DateTime? PasswordExpiry { get; set; }

    public virtual Perfile Perfil { get; set; } = null!;
}
