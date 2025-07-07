namespace app_salvamentos.Models
{
    public class NuevoUsuarioDto
    {
        public string UsuarioLogin { get; set; }
        public string UsuarioEmail { get; set; }
        public string UsuarioPasswordHash { get; set; }
        public int PerfilId { get; set; }
        public Guid PasswordSalt { get; set; }
    }
}
