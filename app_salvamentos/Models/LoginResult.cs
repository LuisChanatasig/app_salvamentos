namespace app_salvamentos.Models
{
    public class LoginResult
    {
        public int Codigo { get; set; } // 0=OK,1=no existe,2=bloqueado,3=clave incorrecta,4=expirada
        public int? UsuarioId { get; set; } // Usar int? para permitir NULL si el usuario no existe
        public int? PerfilId { get; set; } // Usar int?
        public string UsuarioLogin { get; set; }
        public string PerfilNombre { get; set; }

        // Nuevas propiedades para el hash y salt obtenidos de la DB
        public string StoredPasswordHash { get; set; }
        public Guid? StoredPasswordSalt { get; set; } // Guid? para permitir NULL

        // Nuevas propiedades para el estado de seguridad del usuario
        public int? FailedLoginCount { get; set; }
        public DateTime? LockoutUntil { get; set; }
        public DateTime? PasswordExpiry { get; set; }
    }
}
