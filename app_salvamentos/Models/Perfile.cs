using System;
using System.Collections.Generic;

namespace app_salvamentos.Models;

public partial class Perfile
{
    public int PerfilId { get; set; }

    public string PerfilNombre { get; set; } = null!;

    public string? PerfilDescripcion { get; set; }

    public bool PerfilEstado { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
