using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using app_salvamentos.Models;

namespace app_salvamentos.Servicios
{
    public class AutenticacionService
    {
        private readonly IDbConnection _db;

        public AutenticacionService(IDbConnection dbConnection)
        {
            _db = dbConnection;
        }

        /// <summary>
        /// Llama al SP sp_validar_credenciales_por_email y devuelve el resultado de login.
        /// </summary>
        public async Task<LoginResult> ValidarCredencialesAsync(string correo, string password)
        {
            var p = new DynamicParameters();
            p.Add("@correo", correo, DbType.String, size: 255);
            p.Add("@password", password, DbType.String, size: 100);
            p.Add("@resultado", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("@out_usuario_id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("@out_perfil_id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("@out_usuario_login", dbType: DbType.String, size: 100, direction: ParameterDirection.Output);
            p.Add("@out_perfil_nombre", dbType: DbType.String, size: 100, direction: ParameterDirection.Output);

            await _db.ExecuteAsync(
                "dbo.sp_validar_credenciales_por_email",
                p,
                commandType: CommandType.StoredProcedure
            );

            return new LoginResult
            {
                Codigo = p.Get<int>("@resultado"),
                UsuarioId = p.Get<int>("@out_usuario_id"),
                PerfilId = p.Get<int>("@out_perfil_id"),
                UsuarioLogin = p.Get<string>("@out_usuario_login"),
                PerfilNombre = p.Get<string>("@out_perfil_nombre")
            };
        }
    }
}
