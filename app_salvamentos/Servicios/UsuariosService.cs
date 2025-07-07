using System;
using System.Data; // Necesario para IDbConnection y DbType
using System.Threading.Tasks;
using Dapper; // Asumo que estás usando Dapper para la interacción con la BD
using Microsoft.Extensions.Configuration; // Ya no necesario para la conexión aquí, pero podría serlo para otras cosas
using Microsoft.Extensions.Logging; // Para logging
using Microsoft.Data.SqlClient; // Usar Microsoft.Data.SqlClient para SqlException en .NET moderno
using app_salvamentos.Models; 
// Definición de las clases de excepción personalizadas
public class DuplicadoUsuarioLoginException : Exception
{
    public DuplicadoUsuarioLoginException(string message) : base(message) { }
    public DuplicadoUsuarioLoginException(string message, Exception innerException) : base(message, innerException) { }
}

public class DuplicadoUsuarioEmailException : Exception
{
    public DuplicadoUsuarioEmailException(string message) : base(message) { }
    public DuplicadoUsuarioEmailException(string message, Exception innerException) : base(message, innerException) { }
}

public class ErrorCreacionUsuarioException : Exception
{
    public ErrorCreacionUsuarioException(string message) : base(message) { }
    public ErrorCreacionUsuarioException(string message, Exception innerException) : base(message, innerException) { }
}
public class UsuarioNoEncontradoException : Exception
{
    public UsuarioNoEncontradoException(string message) : base(message) { }
}
public class PerfilNoEncontradoException : Exception
{
    public PerfilNoEncontradoException(string message) : base(message) { }
}
public class ErrorActualizarUsuarioException : Exception
{
    public ErrorActualizarUsuarioException(string message) : base(message) { }
    public ErrorActualizarUsuarioException(string message, Exception innerException) : base(message, innerException) { }
}
public class ErrorEliminarUsuarioException : Exception
{
    public ErrorEliminarUsuarioException(string message) : base(message) { }
    public ErrorEliminarUsuarioException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Excepción personalizada para errores al listar usuarios.
/// </summary>
public class ListarUsuariosException : Exception
{
    public ListarUsuariosException(string message) : base(message) { }
    public ListarUsuariosException(string message, Exception innerException) : base(message, innerException) { }
}


public class UsuarioService // O el nombre de tu clase de servicio/repositorio
{
    // Ahora se inyecta IDbConnection directamente
    private readonly IDbConnection _db;
    private readonly ILogger<UsuarioService> _logger; // Inyectar ILogger

    // Constructor actualizado para inyectar IDbConnection
    public UsuarioService(IDbConnection db, ILogger<UsuarioService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Crea un nuevo usuario en la base de datos.
    /// </summary>
    /// <param name="usuario">Objeto DTO con los datos del nuevo usuario.</param>
    /// <returns>El ID del usuario recién creado.</returns>
    /// <exception cref="DuplicadoUsuarioLoginException">Lanzada si el nombre de usuario ya existe.</exception>
    /// <exception cref="DuplicadoUsuarioEmailException">Lanzada si el correo electrónico ya está registrado.</exception>
    /// <exception cref="ErrorCreacionUsuarioException">Lanzada para errores generales durante la creación del usuario.</exception>
    public async Task<int> CrearNuevoUsuarioAsync(NuevoUsuarioDto usuario)
    {
        const string storedProcedure = "sp_CrearNuevoUsuario";

        // No se usa 'using var connection = new SqlConnection(...)' aquí,
        // ya que la conexión (IDbConnection) se inyecta y su ciclo de vida
        // es gestionado por el contenedor de inyección de dependencias.

        // Configurar los parámetros para el Stored Procedure
        var parameters = new DynamicParameters();
        parameters.Add("@usuario_login", usuario.UsuarioLogin, DbType.String, ParameterDirection.Input, 100);
        parameters.Add("@usuario_email", usuario.UsuarioEmail, DbType.String, ParameterDirection.Input, 255);
        parameters.Add("@usuario_password_hash", usuario.UsuarioPasswordHash, DbType.String, ParameterDirection.Input, 512);
        parameters.Add("@perfil_id", usuario.PerfilId, DbType.Int32);
        parameters.Add("@password_salt", usuario.PasswordSalt, DbType.Guid);

        try
        {
            // Asegúrate de que la conexión esté abierta si no lo está ya por el DI.
            // Dapper a menudo abre la conexión automáticamente si es necesario,
            // pero es buena práctica asegurarse si se realizan múltiples operaciones.
            if (_db.State == ConnectionState.Closed)
            {
                _db.Open();
            }

            // Ejecutar el Stored Procedure y obtener el ID del nuevo usuario
            // QueryFirstOrDefaultAsync es adecuado ya que el SP devuelve un único valor (el ID)
            var nuevoUsuarioId = await _db.QueryFirstOrDefaultAsync<int>(
                storedProcedure,
                parameters,
                commandType: CommandType.StoredProcedure
            );

            // Interpretar el valor de retorno del Stored Procedure
            if (nuevoUsuarioId == -1)
            {
                _logger.LogWarning("Intento de creación de usuario fallido: Login '{Login}' ya existe.", usuario.UsuarioLogin);
                throw new DuplicadoUsuarioLoginException("El nombre de usuario ya existe. Por favor, elija otro.");
            }
            else if (nuevoUsuarioId == -2)
            {
                _logger.LogWarning("Intento de creación de usuario fallido: Email '{Email}' ya existe.", usuario.UsuarioEmail);
                throw new DuplicadoUsuarioEmailException("El correo electrónico ya está registrado. Por favor, utilice otro.");
            }
            else if (nuevoUsuarioId == -99)
            {
                // Este caso podría indicar un error interno en el SP no capturado por RAISERROR específico
                _logger.LogError("Error interno desconocido en el SP 'sp_CrearNuevoUsuario' para el usuario '{Login}'.", usuario.UsuarioLogin);
                throw new ErrorCreacionUsuarioException("Ocurrió un error interno al crear el usuario. Por favor, inténtelo de nuevo más tarde.", null);
            }
            else if (nuevoUsuarioId > 0)
            {
                _logger.LogInformation("Usuario '{Login}' creado exitosamente con ID: {Id}.", usuario.UsuarioLogin, nuevoUsuarioId);
                return nuevoUsuarioId; // Retorna el ID del usuario creado
            }
            else
            {
                // Si el SP no devuelve un ID positivo y no es un código de error conocido
                _logger.LogError("El SP 'sp_CrearNuevoUsuario' no devolvió un ID válido para el usuario '{Login}'. Valor retornado: {Id}", usuario.UsuarioLogin, nuevoUsuarioId);
                throw new ErrorCreacionUsuarioException("El SP no devolvió un ID de usuario válido.", null);
            }
        }
        catch (SqlException ex) // Ahora usando Microsoft.Data.SqlClient.SqlException
        {
            // Captura excepciones específicas de SQL Server.
            // Los RAISERROR del SP se manifiestan como SqlException.
            _logger.LogError(ex, "Error de SQL al intentar crear el usuario '{Login}'. Mensaje: {Message}", usuario.UsuarioLogin, ex.Message);

            // Puedes añadir lógica para mapear SqlException.Number si el SP usa números de error específicos
            // Por ejemplo, si el SP usa un número de error personalizado para duplicados.
            // Para los RAISERROR, el mensaje ya es bastante descriptivo.
            throw new ErrorCreacionUsuarioException($"Error de base de datos al crear usuario: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            // Captura cualquier otra excepción inesperada
            _logger.LogError(ex, "Error inesperado al intentar crear el usuario '{Login}'. Mensaje: {Message}", usuario.UsuarioLogin, ex.Message);
            throw new ErrorCreacionUsuarioException($"Ocurrió un error inesperado al crear el usuario: {ex.Message}", ex);
        }
        finally
        {
            // Es buena práctica cerrar la conexión si se abrió explícitamente aquí
            // o si el ciclo de vida de IDbConnection no es gestionado por el DI para mantenerla abierta.
            // Si el DI gestiona la conexión (ej. AddScoped), no es necesario cerrarla aquí.
            // Para fines de ejemplo y robustez, se incluye una verificación.
            if (_db.State == ConnectionState.Open)
            {
                _db.Close();
            }
        }
    }

    /// <summary>
    /// Lista todos los usuarios con ordenamiento dinámico.
    /// </summary>
    /// <param name="sortColumn">Columna por la que ordenar (ej. 'usuario_login', 'created_at').</param>
    /// <param name="sortDirection">Dirección del ordenamiento ('ASC' o 'DESC').</param>
    /// <returns>Una lista de objetos UsuarioListadoDto.</returns>
    /// <exception cref="ListarUsuariosException">Lanzada si ocurre un error al listar los usuarios.</exception>
    public async Task<IEnumerable<UsuarioListadoDto>> ListarUsuariosAsync(
               string sortColumn = "created_at",
               string sortDirection = "DESC")
    {
        const string storedProcedure = "sp_ListarUsuarios";

        var parameters = new DynamicParameters();
        // Solo se pasan los parámetros de ordenamiento
        parameters.Add("@sort_column", sortColumn, DbType.String, size: 50);
        parameters.Add("@sort_direction", sortDirection, DbType.String, size: 4);

        try
        {
            if (_db.State == ConnectionState.Closed)
            {
                _db.Open();
            }

            _logger.LogInformation("Ejecutando SP '{SPName}' con ordenamiento. Columna: '{SortColumn}', Dirección: '{SortDirection}'",
                storedProcedure, sortColumn, sortDirection);

            // Dapper QueryAsync para leer un único conjunto de resultados
            var usuarios = await _db.QueryAsync<UsuarioListadoDto>(
                storedProcedure,
                parameters,
                commandType: CommandType.StoredProcedure
            );

            _logger.LogInformation("SP '{SPName}' ejecutado exitosamente. Se encontraron {Count} usuarios.",
                storedProcedure, ((List<UsuarioListadoDto>)usuarios).Count);

            return usuarios;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Error de SQL al intentar listar usuarios desde el SP '{SPName}'. Mensaje: {Message}", storedProcedure, ex.Message);
            throw new ListarUsuariosException($"Error de base de datos al listar usuarios: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al intentar listar usuarios desde el SP '{SPName}'. Mensaje: {Message}", storedProcedure, ex.Message);
            throw new ListarUsuariosException($"Ocurrió un error inesperado al listar usuarios: {ex.Message}", ex);
        }
        finally
        {
            if (_db.State == ConnectionState.Open)
            {
                _db.Close();
            }
        }
    }

    /// <summary>
    /// Obtiene los detalles de un usuario por su ID.
    /// </summary>
    /// <param name="usuarioId">El ID del usuario a obtener.</param>
    /// <returns>Un objeto UsuarioDetalleDto si se encuentra el usuario.</returns>
    /// <exception cref="UsuarioNoEncontradoException">Lanzada si el usuario no existe.</exception>
    /// <exception cref="Exception">Lanzada por otros errores inesperados.</exception>
    public async Task<UsuarioListadoDto> ObtenerUsuarioPorIdAsync(int usuarioId)
    {
        const string storedProcedure = "sp_ObtenerUsuarioPorId";

        var parameters = new DynamicParameters();
        parameters.Add("@usuario_id", usuarioId, DbType.Int32);

        try
        {
            if (_db.State == ConnectionState.Closed)
            {
                _db.Open();
            }

            _logger.LogInformation("Ejecutando SP '{SPName}' para obtener usuario ID: {UsuarioId}", storedProcedure, usuarioId);

            var usuario = await _db.QueryFirstOrDefaultAsync<UsuarioListadoDto>(
                storedProcedure,
                parameters,
                commandType: CommandType.StoredProcedure
            );

            if (usuario == null)
            {
                _logger.LogWarning("Usuario con ID {UsuarioId} no encontrado.", usuarioId);
                throw new UsuarioNoEncontradoException($"Usuario con ID {usuarioId} no encontrado.");
            }

            _logger.LogInformation("Usuario con ID {UsuarioId} obtenido exitosamente.", usuarioId);
            return usuario;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Error de SQL al obtener usuario ID {UsuarioId} desde el SP '{SPName}'. Mensaje: {Message}", usuarioId, storedProcedure, ex.Message);
            throw new Exception($"Error de base de datos al obtener usuario: {ex.Message}", ex);
        }
        catch (UsuarioNoEncontradoException)
        {
            throw; // Relanzar la excepción específica
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener usuario ID {UsuarioId} desde el SP '{SPName}'. Mensaje: {Message}", usuarioId, storedProcedure, ex.Message);
            throw new Exception($"Ocurrió un error inesperado al obtener usuario: {ex.Message}", ex);
        }
        finally
        {
            if (_db.State == ConnectionState.Open)
            {
                _db.Close();
            }
        }
    }

    /// <summary>
    /// Edita los datos de un usuario existente.
    /// </summary>
    /// <param name="usuario">Objeto UsuarioEditarDto con los datos actualizados.</param>
    /// <returns>True si la edición fue exitosa.</returns>
    /// <exception cref="UsuarioNoEncontradoException">Lanzada si el usuario no existe.</exception>
    /// <exception cref="DuplicadoUsuarioLoginException">Lanzada si el login ya existe para otro usuario.</exception>
    /// <exception cref="DuplicadoUsuarioEmailException">Lanzada si el email ya existe para otro usuario.</exception>
    /// <exception cref="PerfilNoEncontradoException">Lanzada si el perfil_id no existe.</exception>
    /// <exception cref="ErrorActualizarUsuarioException">Lanzada por otros errores específicos del SP.</exception>
    /// <exception cref="Exception">Lanzada por errores inesperados.</exception>
    public async Task EditarUsuarioAsync(UsuarioEditarDto usuario)
    {
        const string storedProcedure = "sp_EditarUsuario";

        var parameters = new DynamicParameters();
        parameters.Add("@usuario_id", usuario.usuario_id, DbType.Int32);
        parameters.Add("@usuario_login", usuario.usuario_login, DbType.String, size: 100);
        parameters.Add("@usuario_email", usuario.usuario_email, DbType.String, size: 255);
        parameters.Add("@perfil_id", usuario.perfil_id, DbType.Int32);
        parameters.Add("@usuario_estado", usuario.usuario_estado, DbType.Boolean);
        parameters.Add("@resultado", dbType: DbType.Int32, direction: ParameterDirection.Output);

        try
        {
            if (_db.State == ConnectionState.Closed)
            {
                _db.Open();
            }

            _logger.LogInformation("Ejecutando SP '{SPName}' para editar usuario ID: {UsuarioId}", storedProcedure, usuario.usuario_id);

            await _db.ExecuteAsync(
                storedProcedure,
                parameters,
                commandType: CommandType.StoredProcedure
            );

            int resultadoSp = parameters.Get<int>("@resultado");

            switch (resultadoSp)
            {
                case 0:
                    _logger.LogInformation("Usuario ID {UsuarioId} editado exitosamente.", usuario.usuario_id);
                    break;
                case -1:
                    throw new UsuarioNoEncontradoException($"Usuario con ID {usuario.usuario_id} no encontrado para edición.");
                case -2:
                    throw new DuplicadoUsuarioLoginException($"El login '{usuario.usuario_login}' ya está en uso por otro usuario.");
                case -3:
                    throw new DuplicadoUsuarioEmailException($"El correo electrónico '{usuario.usuario_email}' ya está en uso por otro usuario.");
                case -4:
                    throw new PerfilNoEncontradoException($"El perfil con ID {usuario.perfil_id} no existe.");
                case -99:
                    // Si el SP relanza el error, el catch general lo manejará.
                    // Si no lo relanza y solo devuelve -99, puedes lanzar una excepción genérica aquí.
                    throw new ErrorActualizarUsuarioException($"Error interno del SP al editar usuario ID {usuario.usuario_id}.");
                default:
                    throw new ErrorActualizarUsuarioException($"Resultado desconocido del SP al editar usuario ID {usuario.usuario_id}: {resultadoSp}");
            }
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Error de SQL al editar usuario ID {UsuarioId} desde el SP '{SPName}'. Mensaje: {Message}", usuario.usuario_id, storedProcedure, ex.Message);
            throw new ErrorActualizarUsuarioException($"Error de base de datos al editar usuario: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is UsuarioNoEncontradoException || ex is DuplicadoUsuarioLoginException || ex is DuplicadoUsuarioEmailException || ex is PerfilNoEncontradoException || ex is ErrorActualizarUsuarioException)
        {
            throw; // Relanzar las excepciones específicas
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al editar usuario ID {UsuarioId} desde el SP '{SPName}'. Mensaje: {Message}", usuario.usuario_id, storedProcedure, ex.Message);
            throw new Exception($"Ocurrió un error inesperado al editar usuario: {ex.Message}", ex);
        }
        finally
        {
            if (_db.State == ConnectionState.Open)
            {
                _db.Close();
            }
        }
    }

    /// <summary>
    /// Elimina (realiza un soft delete) un usuario por su ID.
    /// </summary>
    /// <param name="usuarioId">El ID del usuario a eliminar.</param>
    /// <returns>True si la eliminación fue exitosa.</returns>
    /// <exception cref="UsuarioNoEncontradoException">Lanzada si el usuario no existe.</exception>
    /// <exception cref="ErrorEliminarUsuarioException">Lanzada por otros errores específicos del SP.</exception>
    /// <exception cref="Exception">Lanzada por errores inesperados.</exception>
    public async Task EliminarUsuarioAsync(int usuarioId)
    {
        const string storedProcedure = "sp_EliminarUsuario";

        var parameters = new DynamicParameters();
        parameters.Add("@usuario_id", usuarioId, DbType.Int32);
        parameters.Add("@resultado", dbType: DbType.Int32, direction: ParameterDirection.Output);

        try
        {
            if (_db.State == ConnectionState.Closed)
            {
                _db.Open();
            }

            _logger.LogInformation("Ejecutando SP '{SPName}' para eliminar usuario ID: {UsuarioId}", storedProcedure, usuarioId);

            await _db.ExecuteAsync(
                storedProcedure,
                parameters,
                commandType: CommandType.StoredProcedure
            );

            int resultadoSp = parameters.Get<int>("@resultado");

            switch (resultadoSp)
            {
                case 0:
                    _logger.LogInformation("Usuario ID {UsuarioId} eliminado (soft delete) exitosamente.", usuarioId);
                    break;
                case -1:
                    throw new UsuarioNoEncontradoException($"Usuario con ID {usuarioId} no encontrado para eliminar.");
                case -99:
                    throw new ErrorEliminarUsuarioException($"Error interno del SP al eliminar usuario ID {usuarioId}.");
                default:
                    throw new ErrorEliminarUsuarioException($"Resultado desconocido del SP al eliminar usuario ID {usuarioId}: {resultadoSp}");
            }
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Error de SQL al eliminar usuario ID {UsuarioId} desde el SP '{SPName}'. Mensaje: {Message}", usuarioId, storedProcedure, ex.Message);
            throw new ErrorEliminarUsuarioException($"Error de base de datos al eliminar usuario: {ex.Message}", ex);
        }
        catch (UsuarioNoEncontradoException)
        {
            throw; // Relanzar la excepción específica
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al eliminar usuario ID {UsuarioId} desde el SP '{SPName}'. Mensaje: {Message}", usuarioId, storedProcedure, ex.Message);
            throw new Exception($"Ocurrió un error inesperado al eliminar usuario: {ex.Message}", ex);
        }
        finally
        {
            if (_db.State == ConnectionState.Open)
            {
                _db.Close();
            }
        }
    }
}
