namespace app_salvamentos.Helper
{
    public static class UsuarioSesionHelper
    {
        private const string KeyUsuarioId = "UsuarioId";
        private const string KeyRolId = "RolId";
        private const string KeyUsuarioLogin = "UsuarioLogin";
        private const string KeyPerfilNombre = "PerfilNombre";
        private const string KeyNombres = "Nombres";

        /// <summary>
        /// Devuelve el ID del usuario almacenado en sesión (o 0 si no existe).
        /// </summary>
        public static int UsuarioId(HttpContext context) =>
            context.Session.GetInt32(KeyUsuarioId) ?? 0;

        /// <summary>
        /// Devuelve el ID del rol del usuario en sesión (o 0 si no existe).
        /// </summary>
        public static int RolId(HttpContext context) =>
            context.Session.GetInt32(KeyRolId) ?? 0;

        /// <summary>
        /// Devuelve el login (usuario) almacenado en sesión (o cadena vacía si no existe).
        /// </summary>
        public static string UsuarioLogin(HttpContext context) =>
            context.Session.GetString(KeyUsuarioLogin) ?? string.Empty;

        /// <summary>
        /// Devuelve el nombre del perfil almacenado en sesión (o cadena vacía si no existe).
        /// </summary>
        public static string PerfilNombre(HttpContext context) =>
            context.Session.GetString(KeyPerfilNombre) ?? string.Empty;

        /// <summary>
        /// Devuelve el nombre completo del usuario (o cadena vacía si no existe).
        /// </summary>
        public static string Nombres(HttpContext context) =>
            context.Session.GetString(KeyNombres) ?? string.Empty;
    }
}
