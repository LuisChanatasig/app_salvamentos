namespace app_salvamentos.Models
{
    public class UsuarioEditarDto
    {
        public int usuario_id { get; set; }
        public string usuario_login { get; set; }
        public string usuario_email { get; set; }
        public int perfil_id { get; set; }
        public bool usuario_estado { get; set; }
    }
}
