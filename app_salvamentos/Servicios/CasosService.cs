using app_salvamentos.Configuration;
using app_salvamentos.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;

namespace app_salvamentos.Servicios
{
    // ======================================================================================================
    // Excepciones Personalizadas (Para un manejo de errores más específico)
    // ======================================================================================================

    public class CasoCreationException : Exception
    {
        public int ErrorCode { get; }
        public CasoCreationException(string message, int errorCode) : base(message) { ErrorCode = errorCode; }
        public CasoCreationException(string message, int errorCode, Exception innerException) : base(message, innerException) { ErrorCode = errorCode; }
    }

    public class UsuarioAuditoriaInvalidoException : CasoCreationException
    {
        public UsuarioAuditoriaInvalidoException(string message) : base(message, -5) { }
    }
    public class IdentificacionAseguradoDuplicadaException : CasoCreationException
    {
        public IdentificacionAseguradoDuplicadaException(string message) : base(message, -10) { }
    }
    public class PlacaVehiculoDuplicadaException : CasoCreationException
    {
        public PlacaVehiculoDuplicadaException(string message) : base(message, -20) { }
    }
    public class NumeroChasisVehiculoDuplicadoException : CasoCreationException
    {
        public NumeroChasisVehiculoDuplicadoException(string message) : base(message, -21) { }
    }
    public class NumeroMotorVehiculoDuplicadoException : CasoCreationException
    {
        public NumeroMotorVehiculoDuplicadoException(string message) : base(message, -22) { }
    }

    public class EstadoCasoInvalidoException : CasoCreationException
    {
        public EstadoCasoInvalidoException(string message) : base(message, -31) { }
    }
    public class TipoDocumentoAseguradoInvalidoException : CasoCreationException
    {
        public TipoDocumentoAseguradoInvalidoException(string message) : base(message, -41) { }
    }
    public class TipoDocumentoCasoInvalidoException : CasoCreationException
    {
        public TipoDocumentoCasoInvalidoException(string message) : base(message, -42) { }
    }
    public class ErrorInternoSPCasoException : CasoCreationException
    {
        public ErrorInternoSPCasoException(string message) : base(message, -99) { }
        public ErrorInternoSPCasoException(string message, Exception innerException) : base(message, -99, innerException) { }
    }

    // Excepción personalizada para listar casos
    public class ListarCasosException : Exception
    {
        public ListarCasosException(string message) : base(message) { }
        public ListarCasosException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Excepción personalizada para cuando un caso no es encontrado.
    /// </summary>
    public class CasoNoEncontradoException : Exception
    {
        public CasoNoEncontradoException(string message) : base(message) { }
        public CasoNoEncontradoException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Excepción personalizada para errores generales al interactuar con casos.
    /// </summary>
    public class CasoServiceException : Exception
    {
        public CasoServiceException(string message) : base(message) { }
        public CasoServiceException(string message, Exception innerException) : base(message, innerException) { }
    }
    public class CasoModificationException : Exception
    {
        public int ResultCode { get; }
        public CasoModificationException(string message, int resultCode) : base(message) { ResultCode = resultCode; }
    }
    public class CasoNotFoundException : Exception { public CasoNotFoundException(string message) : base(message) { } }

    public class CasosService
    {
        private readonly IDbConnection _db;
        private readonly ILogger<CasosService> _logger;
        private readonly AppAutopiezasContext _dbContext;
        private readonly FileStorageSettings _fileStorageSettings;

        public CasosService(IDbConnection db, ILogger<CasosService> logger, AppAutopiezasContext dbContext, IOptions<FileStorageSettings> fileStorageOptions)
        {
            _db = db;
            _logger = logger;
            _dbContext = dbContext;
            _fileStorageSettings = fileStorageOptions.Value; // ✅ ESTO AHORA FUNCIONA

        }

        /// <summary>
        /// Crea un caso completo, incluyendo asegurado, vehículo y documentos asociados.
        /// </summary>
        /// <param name="dto">Objeto DTO con todos los datos necesarios para la creación del caso.</param>
        /// <param name="usuarioId">El ID del usuario que realiza la operación (para auditoría).</param>
        /// <returns>El ID del caso recién creado.</returns>
        /// <exception cref="CasoCreationException">Lanzada para errores específicos durante la creación del caso.</exception>
        /// <exception cref="Exception">Lanzada para errores inesperados.</exception>

        public async Task<int> CrearCasoCompletoAsync(CrearCasoDto dto, int usuarioId)
        {
            const string storedProcedure = "sp_CrearCasoCompleto";

            // Crear DataTable para documentos del Asegurado (TVP)
            var documentosAseguradoTable = new DataTable();
            documentosAseguradoTable.Columns.Add("tipo_documento_id", typeof(int));
            documentosAseguradoTable.Columns.Add("nombre_archivo", typeof(string));
            documentosAseguradoTable.Columns.Add("ruta_fisica", typeof(string));
            documentosAseguradoTable.Columns.Add("observaciones", typeof(string));

            foreach (var doc in dto.DocumentosAsegurado)
            {
                documentosAseguradoTable.Rows.Add(doc.TipoDocumentoId, doc.NombreArchivo, doc.RutaFisica, doc.Observaciones);
            }

            // Crear DataTable para documentos del Caso (TVP)
            var documentosCasoTable = new DataTable();
            documentosCasoTable.Columns.Add("tipo_documento_id", typeof(int));
            documentosCasoTable.Columns.Add("nombre_archivo", typeof(string));
            documentosCasoTable.Columns.Add("ruta_fisica", typeof(string));
            documentosCasoTable.Columns.Add("observaciones", typeof(string));

            foreach (var doc in dto.DocumentosCaso)
            {
                documentosCasoTable.Rows.Add(doc.TipoDocumentoId, doc.NombreArchivo, doc.RutaFisica, doc.Observaciones);
            }

            var parameters = new DynamicParameters();

            // Parámetros del Asegurado
            parameters.Add("@nombre_completo", dto.NombreCompleto);
            parameters.Add("@identificacion", dto.Identificacion);
            parameters.Add("@telefono", dto.Telefono);
            parameters.Add("@email", dto.Email);
            parameters.Add("@direccion", dto.Direccion);

            // Parámetros del Vehículo
            parameters.Add("@placa", dto.Placa);
            parameters.Add("@marca", dto.Marca);
            parameters.Add("@modelo", dto.Modelo);
            parameters.Add("@transmision", dto.Transmision);
            parameters.Add("@combustible", dto.Combustible);
            parameters.Add("@cilindraje", dto.Cilindraje);
            parameters.Add("@anio", dto.Anio);
            parameters.Add("@numero_chasis", dto.NumeroChasis);
            parameters.Add("@numero_motor", dto.NumeroMotor);
            parameters.Add("@tipo_vehiculo", dto.TipoVehiculo);
            parameters.Add("@clase", dto.Clase);
            parameters.Add("@color", dto.Color);
            parameters.Add("@observaciones_vehiculo", dto.ObservacionesVehiculo);

            // Parámetros del Caso
            parameters.Add("@numero_reclamo", dto.NumeroReclamo);
            parameters.Add("@fecha_siniestro", dto.FechaSiniestro);
            parameters.Add("@caso_estado_id", dto.CasoEstadoId);

            // Parámetros de Documentos (TVP)
            parameters.Add("@documentos_asegurado", documentosAseguradoTable.AsTableValuedParameter("dbo.DocumentoAseguradoTableType"));
            parameters.Add("@documentos_caso", documentosCasoTable.AsTableValuedParameter("dbo.DocumentoCasoTableType"));

            // Parámetro de Auditoría
            parameters.Add("@usuario_id", usuarioId);

            // Parámetros de Salida del SP
            parameters.Add("@resultado", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@nuevo_caso_id", dbType: DbType.Int32, direction: ParameterDirection.Output);

            try
            {
                if (_db.State == ConnectionState.Closed)
                {
                    _db.Open();
                }

                _logger.LogInformation("Ejecutando SP '{SPName}' para crear caso con reclamo: {NumeroReclamo}", storedProcedure, dto.NumeroReclamo);

                await _db.QueryAsync(storedProcedure, parameters, commandType: CommandType.StoredProcedure);


                var resultadoSp = parameters.Get<int>("@resultado");
                var nuevoCasoId = parameters.Get<int>("@nuevo_caso_id");

                _logger.LogInformation("SP '{SPName}' ejecutado. Resultado: {Resultado}, Nuevo Caso ID: {NuevoCasoId}", storedProcedure, resultadoSp, nuevoCasoId);

                switch (resultadoSp)
                {
                    // CAMBIO: El SP ahora devuelve 1 para éxito
                    case 1:
                        if (nuevoCasoId <= 0)
                        {
                            _logger.LogError("SP '{SPName}' devolvió resultado 1 (éxito) pero nuevo_caso_id es inválido: {NuevoCasoId}", storedProcedure, nuevoCasoId);
                            throw new ErrorInternoSPCasoException("El SP indicó éxito, pero el ID del caso creado no es válido.");
                        }
                        return nuevoCasoId;
                    case -5:
                        throw new UsuarioAuditoriaInvalidoException("El usuario de auditoría proporcionado no es válido.");
                    case -10:
                        throw new IdentificacionAseguradoDuplicadaException($"La identificación '{dto.Identificacion}' ya está registrada para otro asegurado.");
                    case -20:
                        throw new PlacaVehiculoDuplicadaException($"La placa '{dto.Placa}' ya está registrada para otro vehículo.");
                    case -21:
                        throw new NumeroChasisVehiculoDuplicadoException($"El número de chasis '{dto.NumeroChasis}' ya está registrado para otro vehículo.");
                    case -22:
                        throw new NumeroMotorVehiculoDuplicadoException($"El número de motor '{dto.NumeroMotor}' ya está registrado para otro vehículo.");
                    case -31:
                        throw new EstadoCasoInvalidoException($"El estado de caso con ID '{dto.CasoEstadoId}' no es válido o está inactivo.");
                    case -41:
                        throw new TipoDocumentoAseguradoInvalidoException("Uno o más tipos de documento para el asegurado no son válidos o no pertenecen al ámbito 'ASEGURADO'.");
                    case -42:
                        throw new TipoDocumentoCasoInvalidoException("Uno o más tipos de documento para el caso no son válidos o no pertenecen al ámbito 'CASO'.");
                    case -99:
                        throw new ErrorInternoSPCasoException("Ocurrió un error interno inesperado en el Stored Procedure al crear el caso.");
                    default:
                        throw new CasoCreationException($"Error desconocido al crear el caso. Código de resultado del SP: {resultadoSp}", resultadoSp);
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error de SQL al intentar crear el caso con reclamo '{NumeroReclamo}'. Mensaje: {Message}", dto.NumeroReclamo, ex.Message);
                throw new Exception($"Error de base de datos al crear el caso: {ex.Message}", ex);
            }
            catch (CasoCreationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al intentar crear el caso con reclamo '{NumeroReclamo}'. Mensaje: {Message}", dto.NumeroReclamo, ex.Message);
                throw new Exception($"Ocurrió un error inesperado al crear el caso: {ex.Message}", ex);
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
        /// 
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="usuarioId"></param>
        /// <returns></returns>
        /// <exception cref="CasoNotFoundException"></exception>
        /// <exception cref="UsuarioAuditoriaInvalidoException"></exception>
        /// <exception cref="IdentificacionAseguradoDuplicadaException"></exception>
        /// <exception cref="PlacaVehiculoDuplicadaException"></exception>
        /// <exception cref="NumeroChasisVehiculoDuplicadoException"></exception>
        /// <exception cref="NumeroMotorVehiculoDuplicadoException"></exception>
        /// <exception cref="EstadoCasoInvalidoException"></exception>
        /// <exception cref="ErrorInternoSPCasoException"></exception>
        /// <exception cref="CasoModificationException"></exception>
        /// <exception cref="Exception"></exception>
        public async Task<(int resultado, string mensajeCambios)> ModificarCasoCompletoAsync(ModificarCasoDto dto, int usuarioId)
        {
            const string storedProcedure = "sp_ModificarCasoCompleto";

            // Crear DataTable para documentos del Asegurado (TVP)
            var documentosAseguradoTable = new DataTable();
            documentosAseguradoTable.Columns.Add("TipoDocumentoId", typeof(int));
            documentosAseguradoTable.Columns.Add("NombreArchivo", typeof(string));
            documentosAseguradoTable.Columns.Add("RutaFisica", typeof(string));
            documentosAseguradoTable.Columns.Add("Observaciones", typeof(string));

            if (dto.DocumentosAsegurado != null)
            {
                foreach (var doc in dto.DocumentosAsegurado)
                {
                    documentosAseguradoTable.Rows.Add(
                        doc.TipoDocumentoId,
                        doc.NombreArchivo,
                        doc.RutaFisica,
                        doc.Observaciones
                    );
                }
            }

            // Crear DataTable para documentos del Caso (TVP)
            var documentosCasoTable = new DataTable();
            documentosCasoTable.Columns.Add("TipoDocumentoId", typeof(int));
            documentosCasoTable.Columns.Add("NombreArchivo", typeof(string));
            documentosCasoTable.Columns.Add("RutaFisica", typeof(string));
            documentosCasoTable.Columns.Add("Observaciones", typeof(string));

            if (dto.DocumentosCaso != null)
            {
                foreach (var doc in dto.DocumentosCaso)
                {
                    documentosCasoTable.Rows.Add(
                        doc.TipoDocumentoId,
                        doc.NombreArchivo,
                        doc.RutaFisica,
                        doc.Observaciones
                    );
                }
            }

            var parameters = new DynamicParameters();

            // Parameters for the stored procedure
            parameters.Add("@caso_id", dto.CasoId);
            parameters.Add("@nombre_completo", dto.NombreCompleto);
            parameters.Add("@identificacion", dto.Identificacion);
            parameters.Add("@telefono", dto.Telefono);
            parameters.Add("@email", dto.Email);
            parameters.Add("@direccion", dto.Direccion);

            parameters.Add("@placa", dto.Placa);
            parameters.Add("@marca", dto.Marca);
            parameters.Add("@modelo", dto.Modelo);
            parameters.Add("@transmision", dto.Transmision);
            parameters.Add("@combustible", dto.Combustible);
            parameters.Add("@cilindraje", dto.Cilindraje);
            parameters.Add("@anio", dto.Anio);
            parameters.Add("@numero_chasis", dto.NumeroChasis);
            parameters.Add("@numero_motor", dto.NumeroMotor);
            parameters.Add("@tipo_vehiculo", dto.TipoVehiculo);
            parameters.Add("@clase", dto.Clase);
            parameters.Add("@color", dto.Color);
            parameters.Add("@observaciones_vehiculo", dto.ObservacionesVehiculo);

            parameters.Add("@numero_reclamo", dto.NumeroReclamo);
            parameters.Add("@fecha_siniestro", dto.FechaSiniestro);
            parameters.Add("@caso_estado_id", dto.CasoEstadoId);

            // Table-Valued Parameters (TVP) for documents
            parameters.Add("@documentos_asegurado", documentosAseguradoTable.AsTableValuedParameter("dbo.DocumentoAseguradoTableType"));
            parameters.Add("@documentos_caso", documentosCasoTable.AsTableValuedParameter("dbo.DocumentoCasoTableType"));

            parameters.Add("@usuario_id", usuarioId);

            // Output parameters from the stored procedure
            parameters.Add("@resultado", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@mensaje_cambios", dbType: DbType.String, direction: ParameterDirection.Output, size: -1); // -1 for NVARCHAR(MAX)

            try
            {
                if (_db.State == ConnectionState.Closed)
                {
                    await ((System.Data.Common.DbConnection)_db).OpenAsync();
                }

                _logger.LogInformation("Ejecutando SP '{SPName}' para modificar caso ID: {CasoId}", storedProcedure, dto.CasoId);

                await _db.ExecuteAsync(storedProcedure, parameters, commandType: CommandType.StoredProcedure);

                var resultadoSp = parameters.Get<int>("@resultado");
                var mensajeCambios = parameters.Get<string>("@mensaje_cambios");

                _logger.LogInformation("SP '{SPName}' ejecutado. Resultado: {Resultado}, Mensaje Cambios: {MensajeCambios}", storedProcedure, resultadoSp, mensajeCambios);

                switch (resultadoSp)
                {
                    case 1:
                        return (resultadoSp, mensajeCambios); // Success
                    case -1:
                        throw new CasoNotFoundException($"El caso con ID '{dto.CasoId}' no fue encontrado.");
                    case -5:
                        throw new UsuarioAuditoriaInvalidoException("El usuario de auditoría proporcionado no es válido.");
                    case -10:
                        throw new IdentificacionAseguradoDuplicadaException($"La identificación '{dto.Identificacion}' ya está registrada para otro asegurado.");
                    case -20:
                        throw new PlacaVehiculoDuplicadaException($"La placa '{dto.Placa}' ya está registrada para otro vehículo.");
                    case -21:
                        throw new NumeroChasisVehiculoDuplicadoException($"El número de chasis '{dto.NumeroChasis}' ya está registrado para otro vehículo.");
                    case -22:
                        throw new NumeroMotorVehiculoDuplicadoException($"El número de motor '{dto.NumeroMotor}' ya está registrado para otro vehículo.");
                    case -31:
                        throw new EstadoCasoInvalidoException($"El estado de caso con ID '{dto.CasoEstadoId}' no es válido o está inactivo.");
                    case -99:
                        throw new ErrorInternoSPCasoException("Ocurrió un error interno inesperado en el Stored Procedure al modificar el caso.");
                    default:
                        throw new CasoModificationException($"Error desconocido al modificar el caso. Código de resultado del SP: {resultadoSp}", resultadoSp);
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error de SQL al intentar modificar el caso con ID '{CasoId}'. Mensaje: {Message}", dto.CasoId, ex.Message);
                throw new Exception($"Error de base de datos al modificar el caso: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                if (ex is UsuarioAuditoriaInvalidoException || ex is IdentificacionAseguradoDuplicadaException ||
                    ex is PlacaVehiculoDuplicadaException || ex is NumeroChasisVehiculoDuplicadoException ||
                    ex is NumeroMotorVehiculoDuplicadoException || ex is EstadoCasoInvalidoException ||
                    ex is ErrorInternoSPCasoException || ex is CasoModificationException ||
                    ex is CasoNotFoundException)
                {
                    _logger.LogWarning(ex, "Advertencia al modificar el caso ID '{CasoId}': {Message}", dto.CasoId, ex.Message);
                    throw;
                }

                _logger.LogError(ex, "Error inesperado al intentar modificar el caso con ID '{CasoId}'. Mensaje: {Message}", dto.CasoId, ex.Message);
                throw new Exception($"Ocurrió un error inesperado al modificar el caso: {ex.Message}", ex);
            }
            finally
            {
                if (_db.State == ConnectionState.Open)
                {
                    await ((System.Data.Common.DbConnection)_db).CloseAsync();
                }
            }
        }

        /// <summary>
        /// Desactiva un documento de la base de datos (borrado lógico), registra la acción en el histórico
        /// y devuelve la ruta física del archivo asociado (por si se desea borrar físicamente).
        /// </summary>
        /// <param name="documentoId">El ID del documento a desactivar.</param>
        /// <param name="usuarioId">El ID del usuario que realiza la acción (para auditoría).</param>
        /// <returns>
        /// Una tupla con:
        /// - resultado: 1 = éxito, 0 = no encontrado o no actualizado, -1 = error de DB, -2 = error general.
        /// - mensaje descriptivo del resultado.
        /// - ruta física del archivo, útil si se desea eliminarlo del sistema de archivos.
        /// </returns>

        public async Task<(int Resultado, string Mensaje, string? RutaFisicaAntigua)> BorrarDocumentoAsync(int documentoId, int usuarioId)
        {
            int resultado = 0;
            string mensaje = string.Empty;
            string? rutaFisicaAntigua = null; // Variable para almacenar la ruta física del documento a borrar

            try
            {
                using (SqlConnection conn = new SqlConnection(_dbContext.Database.GetConnectionString()))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_BorrarDocumento", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@DocumentoId", documentoId);
                        // Mapea el usuarioId del C# al @UsuarioAccion del SP
                        cmd.Parameters.AddWithValue("@UsuarioAccion", usuarioId);

                        await conn.OpenAsync();

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync()) // Lee la fila de resultados del SP
                            {
                                resultado = reader.GetInt32(reader.GetOrdinal("Resultado"));
                                mensaje = reader.GetString(reader.GetOrdinal("Mensaje"));

                                // Intenta obtener la RutaFisicaAntigua si el SP la devuelve y no es DBNull
                                int rutaFisicaAntiguaOrdinal = reader.GetOrdinal("RutaFisicaAntigua");
                                if (rutaFisicaAntiguaOrdinal >= 0 && !reader.IsDBNull(rutaFisicaAntiguaOrdinal))
                                {
                                    rutaFisicaAntigua = reader.GetString(rutaFisicaAntiguaOrdinal);
                                }
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error de SQL al intentar borrar documento con ID {DocumentoId} por usuario {UsuarioId}.", documentoId, usuarioId);
                mensaje = "Error de base de datos al borrar el documento.";
                resultado = -1; // Código de error para problemas de DB
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al intentar borrar documento con ID {DocumentoId} por usuario {UsuarioId}.", documentoId, usuarioId);
                mensaje = "Error interno al borrar el documento.";
                resultado = -2; // Código de error para otros problemas
            }

            return (resultado, mensaje, rutaFisicaAntigua);
        }
        /// <summary>
        /// Lista todos los casos con ordenamiento dinámico.
        /// </summary>
        /// <param name="sortColumn">Columna por la que ordenar (ej. 'numero_avaluo', 'created_at').</param>
        /// <param name="sortDirection">Dirección del ordenamiento ('ASC' o 'DESC').</param>
        /// <returns>Una lista de objetos CasoListadoDto.</returns>
        /// <exception cref="ListarCasosException">Lanzada si ocurre un error al listar los casos.</exception>
        public async Task<IEnumerable<CasoListadoDto>> ListarCasosAsync(
            string sortColumn = "created_at",
            string sortDirection = "DESC")
        {
            const string storedProcedure = "sp_ListarCasos"; // Nombre de tu Stored Procedure

            // Configurar los parámetros para el Stored Procedure
            var parameters = new DynamicParameters();
            parameters.Add("@sort_column", sortColumn, DbType.String, size: 50);
            parameters.Add("@sort_direction", sortDirection, DbType.String, size: 4);

            try
            {
                // Asegúrate de que la conexión esté abierta.
                // Dapper a menudo abre la conexión automáticamente si es necesario,
                // pero es buena práctica asegurarse si se realizan múltiples operaciones.
                if (_db.State == ConnectionState.Closed)
                {
                    _db.Open();
                }

                _logger.LogInformation("Ejecutando SP '{SPName}' con ordenamiento. Columna: '{SortColumn}', Dirección: '{SortDirection}'",
                    storedProcedure, sortColumn, sortDirection);

                // Ejecutar el Stored Procedure y mapear los resultados a CasoListadoDto
                var casos = await _db.QueryAsync<CasoListadoDto>(
                    storedProcedure,
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                // Convertir a lista para poder obtener el conteo, si es necesario.
                // Si el resultado es grande, considera usar .AsList() o .ToArray() si necesitas múltiples enumeraciones.
                var casosList = casos.ToList();

                _logger.LogInformation("SP '{SPName}' ejecutado exitosamente. Se encontraron {Count} casos.",
                    storedProcedure, casosList.Count);

                return casosList;
            }
            catch (SqlException ex) // Captura excepciones específicas de SQL Server
            {
                _logger.LogError(ex, "Error de SQL al intentar listar casos desde el SP '{SPName}'. Mensaje: {Message}", storedProcedure, ex.Message);
                // Envuelve la excepción SQL en una excepción de capa de servicio más amigable
                throw new ListarCasosException($"Error de base de datos al listar casos: {ex.Message}", ex);
            }
            catch (Exception ex) // Captura cualquier otra excepción inesperada
            {
                _logger.LogError(ex, "Error inesperado al intentar listar casos desde el SP '{SPName}'. Mensaje: {Message}", storedProcedure, ex.Message);
                // Envuelve la excepción en una excepción de capa de servicio más amigable
                throw new ListarCasosException($"Ocurrió un error inesperado al listar casos: {ex.Message}", ex);
            }
            finally
            {
                // Es buena práctica cerrar la conexión si se abrió explícitamente aquí
                // o si el ciclo de vida de IDbConnection no es gestionado por el DI para mantenerla abierta.
                if (_db.State == ConnectionState.Open)
                {
                    _db.Close();
                }
            }
        }


        /// <summary>
        /// Lista todos los casos con ordenamiento dinámico.
        /// </summary>
        /// <param name="sortColumn">Columna por la que ordenar (ej. 'numero_avaluo', 'created_at').</param>
        /// <param name="sortDirection">Dirección del ordenamiento ('ASC' o 'DESC').</param>
        /// <returns>Una lista de objetos CasoListadoDto.</returns>
        /// <exception cref="ListarCasosException">Lanzada si ocurre un error al listar los casos.</exception>
        public async Task<IEnumerable<CasoListadoDto>> ListarCasosEstadoAsync(
      string sortColumn = "created_at",
      string sortDirection = "DESC",
      int? estado_id = null) // Hacemos el estado_id nullable para que coincida con el SP (NULL por defecto)
        {
            // Usamos el nombre del SP que modificamos
            const string storedProcedure = "sp_ListarCasosEstado";

            // Configurar los parámetros para el Stored Procedure
            var parameters = new DynamicParameters();
            parameters.Add("@sort_column", sortColumn, DbType.String, size: 50);
            parameters.Add("@sort_direction", sortDirection, DbType.String, size: 4);

            // CORRECCIÓN: Nombre del parámetro y tipo de dato
            parameters.Add("@estado_id", estado_id, DbType.Int32); // Usamos DbType.Int32 para un int

            try
            {
                // Asegúrate de que la conexión esté abierta.
                // Dapper a menudo abre la conexión automáticamente si es necesario,
                // pero es buena práctica asegurarse si se realizan múltiples operaciones.
                if (_db.State == ConnectionState.Closed)
                {
                    _db.Open();
                }

                _logger.LogInformation("Ejecutando SP '{SPName}' con ordenamiento y filtro. Columna: '{SortColumn}', Dirección: '{SortDirection}', Estado ID: '{EstadoId}'",
                    storedProcedure, sortColumn, sortDirection, estado_id);

                // Ejecutar el Stored Procedure y mapear los resultados a CasoListadoDto
                var casos = await _db.QueryAsync<CasoListadoDto>(
                    storedProcedure,
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var casosList = casos.ToList();

                _logger.LogInformation("SP '{SPName}' ejecutado exitosamente. Se encontraron {Count} casos.",
                    storedProcedure, casosList.Count);

                return casosList;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error de SQL al intentar listar casos desde el SP '{SPName}'. Mensaje: {Message}", storedProcedure, ex.Message);
                throw new ListarCasosException($"Error de base de datos al listar casos: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al intentar listar casos desde el SP '{SPName}'. Mensaje: {Message}", storedProcedure, ex.Message);
                throw new ListarCasosException($"Ocurrió un error inesperado al listar casos: {ex.Message}", ex);
            }
            finally
            {
                if (_db.State == ConnectionState.Open)
                {
                    _db.Close();
                }
            }
        }

        public string ObtenerRutaPublica(string rutaRelativa)
        {
            if (string.IsNullOrEmpty(rutaRelativa))
                return null;

            var basePath = @"C:\Archivos_imagenes\App_Salvamento";

            // Combina la ruta física completa (evita problemas de barras)
            var rutaFisicaCompleta = Path.Combine(basePath, rutaRelativa);

            // Ahora sí construyes la ruta pública para URL
            var relativePathUrl = rutaRelativa.Replace("\\", "/").TrimStart('/');

            return $"/imagenes/{relativePathUrl}";
        }



        /// <summary>
        /// Retrieves the full details of a case by its ID.
        /// </summary>
        /// <param name="casoId">The ID of the case to retrieve.</param>
        /// <returns>A CasoDetalleDto object if the case is found.</returns>
        /// <exception cref="CasoNoEncontradoException">Thrown if the case does not exist.</exception>
        /// <exception cref="CasoServiceException">Thrown for other unexpected errors.</exception>
        public async Task<CasoDetalleDto> ObtenerCasoPorIdAsync(int casoId)
        {
            const string storedProcedure = "sp_ObtenerCasoPorId";

            var parameters = new DynamicParameters();
            parameters.Add("@caso_id", casoId, DbType.Int32, ParameterDirection.Input);
            parameters.Add("@resultado", dbType: DbType.Int32, direction: ParameterDirection.Output);

            try
            {
                if (_db.State == ConnectionState.Closed)
                    _db.Open();

                _logger.LogInformation("Ejecutando SP '{SPName}' para obtener caso con ID: {CasoId}", storedProcedure, casoId);

                using (var multi = await _db.QueryMultipleAsync(storedProcedure, parameters, commandType: CommandType.StoredProcedure))
                {
                    var caso = await multi.ReadFirstOrDefaultAsync<CasoDetalleDto>();
                    var resumenFinanciero = await multi.ReadFirstOrDefaultAsync<ResumenFinancieroDto>();
                    var valoresComerciales = (await multi.ReadAsync<ValorComercialDto>()).ToList();
                    var danos = (await multi.ReadAsync<DanoDto>()).ToList();
                    var partes = (await multi.ReadAsync<ParteDto>()).ToList();
                    var documentosAsegurado = (await multi.ReadAsync<DocumentoDetalleDto>()).ToList();
                    var documentosCaso = (await multi.ReadAsync<DocumentoDetalleDto>()).ToList();
                    var documentosValorComercial = (await multi.ReadAsync<DocumentoDetalleDto>()).ToList();
                    var documentosDano = (await multi.ReadAsync<DocumentoDetalleDto>()).ToList();
                    var documentosPartes = (await multi.ReadAsync<DocumentoDetalleDto>()).ToList();

                    int? resultadoSp = parameters.Get<int?>("@resultado");

                    if (resultadoSp == null)
                    {
                        _logger.LogError("El SP '{SPName}' no devolvió el parámetro de salida '@resultado'.", storedProcedure);
                        throw new CasoServiceException("El Stored Procedure no devolvió un resultado válido.");
                    }

                    if (resultadoSp == -1 || caso == null)
                    {
                        _logger.LogWarning("Caso con ID '{CasoId}' no encontrado. SP resultado: {ResultadoSP}", casoId, resultadoSp);
                        throw new CasoNoEncontradoException($"Caso con ID '{casoId}' no encontrado.");
                    }

                    if (resultadoSp == -99)
                    {
                        _logger.LogError("Error interno del SP '{SPName}' al obtener caso con ID: {CasoId}. Resultado SP: {ResultadoSP}", storedProcedure, casoId, resultadoSp);
                        throw new CasoServiceException("Error interno del Stored Procedure.");
                    }

                    // Asignar datos relacionados
                    caso.Resumen = resumenFinanciero;
                    caso.ValoresComerciales = valoresComerciales;
                    caso.Danos = danos;
                    caso.Partes = partes;

                    // Asignar ruta pública a documentos
                    void AsignarRutaPublica(List<DocumentoDetalleDto> docs)
                    {
                        if (docs == null) return;
                        foreach (var doc in docs)
                        {
                            if (doc == null) continue;
                            doc.RutaPublica = ObtenerRutaPublica(doc.ruta_fisica);
                        }
                    }

                    AsignarRutaPublica(documentosCaso);
                    AsignarRutaPublica(documentosAsegurado);
                    AsignarRutaPublica(documentosValorComercial);
                    AsignarRutaPublica(documentosDano);
                    AsignarRutaPublica(documentosPartes);

                    caso.DocumentosAsegurado = documentosAsegurado;
                    caso.DocumentosCaso = documentosCaso;
                    caso.DocumentosValorComercial = documentosValorComercial;
                    caso.DocumentosDano = documentosDano;
                    caso.DocumentosPartes = documentosPartes;

                    // --- Normalización para evitar nulls ---

                    caso = caso ?? new CasoDetalleDto();

                    caso.Resumen = caso.Resumen ?? new ResumenFinancieroDto();

                    caso.ValoresComerciales = caso.ValoresComerciales ?? new List<ValorComercialDto>();
                    caso.Danos = caso.Danos ?? new List<DanoDto>();
                    caso.Partes = caso.Partes ?? new List<ParteDto>();

                    caso.DocumentosAsegurado = caso.DocumentosAsegurado ?? new List<DocumentoDetalleDto>();
                    caso.DocumentosCaso = caso.DocumentosCaso ?? new List<DocumentoDetalleDto>();
                    caso.DocumentosValorComercial = caso.DocumentosValorComercial ?? new List<DocumentoDetalleDto>();
                    caso.DocumentosDano = caso.DocumentosDano ?? new List<DocumentoDetalleDto>();
                    caso.DocumentosPartes = caso.DocumentosPartes ?? new List<DocumentoDetalleDto>();

                    void SanitizarDocs(List<DocumentoDetalleDto> docs)
                    {
                        if (docs == null) return;
                        foreach (var doc in docs)
                        {
                            if (doc == null) continue;
                            doc.nombre_archivo ??= "";
                            doc.ruta_fisica ??= "";
                            doc.ambito_documento ??= "";
                            doc.observaciones ??= "";
                            // Añade otras propiedades string si es necesario
                        }
                    }

                    SanitizarDocs(caso.DocumentosAsegurado);
                    SanitizarDocs(caso.DocumentosCaso);
                    SanitizarDocs(caso.DocumentosValorComercial);
                    SanitizarDocs(caso.DocumentosDano);
                    SanitizarDocs(caso.DocumentosPartes);

                    _logger.LogInformation("Caso con ID '{CasoId}' obtenido exitosamente. Resumen: {HasResumen}, Valores Comerciales: {VCCount}, Daños: {DanoCount}, Partes: {PartesCount}, Docs Asegurado: {DocAseg}, Docs Caso: {DocCaso}, Docs Valor Comercial: {DocVC}, Docs Daño: {DocDano}, Docs Partes: {DocPartes}",
                        casoId, caso.Resumen != null, caso.ValoresComerciales.Count, caso.Danos.Count, caso.Partes.Count, caso.DocumentosAsegurado.Count, caso.DocumentosCaso.Count, caso.DocumentosValorComercial.Count, caso.DocumentosDano.Count, caso.DocumentosPartes.Count);

                    return caso;
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error de SQL al obtener caso con ID '{CasoId}' desde SP '{SPName}': {Message}", casoId, storedProcedure, ex.Message);
                throw new CasoServiceException($"Error de base de datos al obtener el caso: {ex.Message}", ex);
            }
            catch (CasoNoEncontradoException) { throw; }
            catch (CasoServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al obtener caso con ID '{CasoId}' desde SP '{SPName}': {Message}", casoId, storedProcedure, ex.Message);
                throw new CasoServiceException($"Error inesperado al obtener el caso: {ex.Message}", ex);
            }
            finally
            {
                if (_db.State == ConnectionState.Open)
                    _db.Close();
            }
        }

        public async Task ActualizarDocumentosCasoAsync(int casoId, List<DocumentoDto> documentosAsegurado, List<DocumentoDto> documentosCaso, int usuarioId)
        {

            using (SqlConnection connection = new SqlConnection(_dbContext.Database.GetConnectionString()))
            {
                await connection.OpenAsync();
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    // Obtener el asegurado_id relacionado con este caso_id
                    int aseguradoId = await GetAseguradoIdByCasoId(casoId, connection, transaction);

                    // Insertar Documentos del Asegurado
                    if (documentosAsegurado != null && documentosAsegurado.Any())
                    {
                        using (SqlCommand command = new SqlCommand("INSERT INTO Documentos (tipo_documento_id, nombre_archivo, ruta_fisica, observaciones, fecha_subida, asegurado_id, caso_id, documento_estado, created_at, updated_at, created_by, updated_by) VALUES (@tipo_documento_id, @nombre_archivo, @ruta_fisica, @observaciones, SYSUTCDATETIME(), @asegurado_id, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), @created_by, @updated_by)", connection, transaction))
                        {
                            command.Parameters.Add("@tipo_documento_id", SqlDbType.Int);
                            command.Parameters.Add("@nombre_archivo", SqlDbType.NVarChar, 255);
                            command.Parameters.Add("@ruta_fisica", SqlDbType.NVarChar, 1000);
                            command.Parameters.Add("@observaciones", SqlDbType.NVarChar, 4000);
                            command.Parameters.Add("@asegurado_id", SqlDbType.Int).Value = aseguradoId; // Asignar aquí
                            command.Parameters.Add("@created_by", SqlDbType.Int).Value = usuarioId;
                            command.Parameters.Add("@updated_by", SqlDbType.Int).Value = usuarioId;

                            foreach (var doc in documentosAsegurado)
                            {
                                command.Parameters["@tipo_documento_id"].Value = doc.TipoDocumentoId;
                                command.Parameters["@nombre_archivo"].Value = doc.NombreArchivo;
                                command.Parameters["@ruta_fisica"].Value = doc.RutaFisica;
                                command.Parameters["@observaciones"].Value = (object)doc.Observaciones ?? DBNull.Value;
                                await command.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    // Insertar Documentos del Caso
                    if (documentosCaso != null && documentosCaso.Any())
                    {
                        using (SqlCommand command = new SqlCommand("INSERT INTO Documentos (tipo_documento_id, nombre_archivo, ruta_fisica, observaciones, fecha_subida, asegurado_id, caso_id, documento_estado, created_at, updated_at, created_by, updated_by) VALUES (@tipo_documento_id, @nombre_archivo, @ruta_fisica, @observaciones, SYSUTCDATETIME(), NULL, @caso_id, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), @created_by, @updated_by)", connection, transaction))
                        {
                            command.Parameters.Add("@tipo_documento_id", SqlDbType.Int);
                            command.Parameters.Add("@nombre_archivo", SqlDbType.NVarChar, 255);
                            command.Parameters.Add("@ruta_fisica", SqlDbType.NVarChar, 1000);
                            command.Parameters.Add("@observaciones", SqlDbType.NVarChar, 4000);
                            command.Parameters.Add("@caso_id", SqlDbType.Int).Value = casoId; // Asignar aquí
                            command.Parameters.Add("@created_by", SqlDbType.Int).Value = usuarioId;
                            command.Parameters.Add("@updated_by", SqlDbType.Int).Value = usuarioId;

                            foreach (var doc in documentosCaso)
                            {
                                command.Parameters["@tipo_documento_id"].Value = doc.TipoDocumentoId;
                                command.Parameters["@nombre_archivo"].Value = doc.NombreArchivo;
                                command.Parameters["@ruta_fisica"].Value = doc.RutaFisica;
                                command.Parameters["@observaciones"].Value = (object)doc.Observaciones ?? DBNull.Value;
                                await command.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger.LogError(ex, "Error al actualizar documentos del caso {CasoId}: {Message}", casoId, ex.Message);
                    throw new CasoCreationException("Error al guardar los documentos relacionados con el caso.", casoId);
                }
            }
        }

        private async Task<int> GetAseguradoIdByCasoId(int casoId, SqlConnection connection, SqlTransaction transaction)
        {
            using (SqlCommand command = new SqlCommand("SELECT asegurado_id FROM Casos WHERE caso_id = @caso_id", connection, transaction))
            {
                command.Parameters.AddWithValue("@caso_id", casoId);
                object result = await command.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result);
                }
                throw new InvalidOperationException($"No se encontró el asegurado_id para el caso_id: {casoId}");
            }
        }

    }
}
