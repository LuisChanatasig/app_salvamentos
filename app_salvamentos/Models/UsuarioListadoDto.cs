namespace app_salvamentos.Models
{
    /// <summary>
    /// DTO para representar un usuario en el listado.
    /// Las propiedades coinciden con los nombres de columna snake_case de la base de datos.
    /// </summary>
    public class UsuarioListadoDto
    {
        public int usuario_id { get; set; }
        public string usuario_login { get; set; }
        public string usuario_email { get; set; }
        public int perfil_id { get; set; }
        public string perfil_nombre { get; set; } // Viene de la tabla perfiles
        public bool usuario_estado { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public DateTime? last_login_at { get; set; } // Puede ser NULL
        public int failed_login_count { get; set; }
        public DateTime? lockout_until { get; set; } // Puede ser NULL
        public DateTime? password_expiry { get; set; } // Puede ser NULL
    }

}
