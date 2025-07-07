using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using app_salvamentos.Models;
using Microsoft.Data.SqlClient;
using BCrypt.Net;

namespace app_salvamentos.Servicios
{
    public class AutenticacionService
    {
        private readonly IDbConnection _db;
        private readonly ILogger<AutenticacionService> _logger;
        public AutenticacionService(IDbConnection dbConnection, ILogger<AutenticacionService> logger)
        {
            _db = dbConnection;
            _logger = logger;
        }

        /// <summary>
        /// Valida las credenciales de un usuario por email.
        /// </summary>
        /// <param name="correo">El correo electrónico del usuario.</param>
        /// <param name="password">La contraseña en texto plano proporcionada por el usuario.</param>
        /// <returns>Un objeto LoginResult que indica el resultado de la autenticación y los datos del usuario.</returns>
        public async Task<LoginResult> ValidarCredencialesAsync(string correo, string password)
        {
            const string storedProcedure = "dbo.sp_validar_credenciales_por_email";

            var p = new DynamicParameters();
            p.Add("@correo", correo, DbType.String, size: 255);
            // Ya no pasamos @password como parámetro de entrada al SP

            // Parámetros de salida del SP
            p.Add("@resultado", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("@out_usuario_id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("@out_perfil_id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("@out_usuario_login", dbType: DbType.String, size: 100, direction: ParameterDirection.Output);
            p.Add("@out_perfil_nombre", dbType: DbType.String, size: 100, direction: ParameterDirection.Output);
            p.Add("@out_password_hash_almacenado", dbType: DbType.String, size: 512, direction: ParameterDirection.Output);
            p.Add("@out_password_salt_almacenado", dbType: DbType.Guid, direction: ParameterDirection.Output);
            p.Add("@out_failed_login_count", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("@out_lockout_until", dbType: DbType.DateTime2, direction: ParameterDirection.Output);
            p.Add("@out_password_expiry", dbType: DbType.DateTime2, direction: ParameterDirection.Output);

            try
            {
                if (_db.State == ConnectionState.Closed)
                {
                    _db.Open();
                }

                await _db.ExecuteAsync(
                    storedProcedure,
                    p,
                    commandType: CommandType.StoredProcedure
                );

                // Recuperar los valores de los parámetros de salida
                var resultadoSp = p.Get<int>("@resultado");
                var usuarioId = p.Get<int?>("@out_usuario_id");
                var perfilId = p.Get<int?>("@out_perfil_id");
                var usuarioLogin = p.Get<string>("@out_usuario_login");
                var perfilNombre = p.Get<string>("@out_perfil_nombre");
                var storedPasswordHash = p.Get<string>("@out_password_hash_almacenado");
                var storedPasswordSalt = p.Get<Guid?>("@out_password_salt_almacenado");
                var failedLoginCount = p.Get<int?>("@out_failed_login_count");
                var lockoutUntil = p.Get<DateTime?>("@out_lockout_until");
                var passwordExpiry = p.Get<DateTime?>("@out_password_expiry");


                var loginResult = new LoginResult
                {
                    Codigo = resultadoSp,
                    UsuarioId = usuarioId,
                    PerfilId = perfilId,
                    UsuarioLogin = usuarioLogin,
                    PerfilNombre = perfilNombre,
                    StoredPasswordHash = storedPasswordHash, // Guardar el hash para verificación
                    StoredPasswordSalt = storedPasswordSalt, // Guardar el salt para referencia (BCrypt lo maneja internamente)
                    FailedLoginCount = failedLoginCount,
                    LockoutUntil = lockoutUntil,
                    PasswordExpiry = passwordExpiry
                };

                // Si el SP ya indicó un problema (no existe, inactivo, bloqueado, expirado),
                // no necesitamos verificar la contraseña.
                if (loginResult.Codigo != 0)
                {
                    _logger.LogWarning("Login fallido para {Correo}: Código SP {Codigo}.", correo, loginResult.Codigo);
                    return loginResult;
                }

                // *** Lógica de verificación de contraseña con BCrypt en C# ***
                // Si el hash almacenado es nulo o la verificación falla
                if (string.IsNullOrEmpty(storedPasswordHash) || !BCrypt.Net.BCrypt.Verify(password, storedPasswordHash))
                {
                    loginResult.Codigo = 3; // Contraseña incorrecta
                    _logger.LogWarning("Intento de login fallido para {Correo}: Contraseña incorrecta.", correo);

                    // Incrementar el contador de intentos fallidos y potencialmente bloquear
                    if (usuarioId.HasValue)
                    {
                        await IncrementarIntentosFallidosYBloquearAsync(usuarioId.Value);
                    }
                }
                else
                {
                    // Contraseña Correcta
                    // Resetear intentos fallidos y actualizar last_login_at
                    if (usuarioId.HasValue)
                    {
                        await ResetearIntentosFallidosYActualizarLoginAsync(usuarioId.Value);
                    }
                    _logger.LogInformation("Login exitoso para {Correo}.", correo);
                }

                return loginResult;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error de SQL al validar credenciales para {Correo}. Mensaje: {Message}", correo, ex.Message);
                throw new Exception($"Error de base de datos al validar credenciales: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al validar credenciales para {Correo}. Mensaje: {Message}", correo, ex.Message);
                throw new Exception($"Ocurrió un error inesperado al validar credenciales: {ex.Message}", ex);
            }
            finally
            {
                if (_db.State == ConnectionState.Open)
                {
                    _db.Close();
                }
            }
        }

        // Métodos auxiliares para actualizar la DB después de la verificación en C#
        // Estos SPs deben ser creados en tu base de datos si no existen.
        private async Task IncrementarIntentosFallidosYBloquearAsync(int usuarioId)
        {
            const string updateSp = "sp_IncrementarIntentosFallidosYBloquear";
            try
            {
                if (_db.State == ConnectionState.Closed) _db.Open();
                await _db.ExecuteAsync(updateSp, new { usuario_id = usuarioId }, commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al incrementar intentos fallidos para usuario ID: {UsuarioId}", usuarioId);
            }
        }

        private async Task ResetearIntentosFallidosYActualizarLoginAsync(int usuarioId)
        {
            const string updateSp = "sp_ResetearIntentosFallidosYActualizarLogin";
            try
            {
                if (_db.State == ConnectionState.Closed) _db.Open();
                await _db.ExecuteAsync(updateSp, new { usuario_id = usuarioId }, commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al resetear intentos fallidos y actualizar login para usuario ID: {UsuarioId}", usuarioId);
            }
        }
    }
}
