namespace app_salvamentos.Models
{
    public class LoginResult
    {
        public int Codigo { get; set; }  // 0=OK,1=no existe,2=bloqueado,3=clave,4=expirada
        public int UsuarioId { get; set; }
        public int PerfilId { get; set; }
        public string UsuarioLogin { get; set; }
        public string PerfilNombre { get; set; }
    }
}
