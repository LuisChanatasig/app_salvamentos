using app_salvamentos.Models;
using Dapper;
using Microsoft.Data.SqlClient;
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
    public class NumeroAvaluoCasoDuplicadoException : CasoCreationException
    {
        public NumeroAvaluoCasoDuplicadoException(string message) : base(message, -30) { }
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
    public class CasosService
    {
        private readonly IDbConnection _db;
        private readonly ILogger<CasosService> _logger;

        public CasosService(IDbConnection db, ILogger<CasosService> logger)
        {
            _db = db;
            _logger = logger;
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
            documentosAseguradoTable.Columns.Add("ruta_fisica", typeof(string)); // CAMBIO: Columna para la ruta
            documentosAseguradoTable.Columns.Add("observaciones", typeof(string));

            foreach (var doc in dto.DocumentosAsegurado)
            {
                documentosAseguradoTable.Rows.Add(doc.TipoDocumentoId, doc.NombreArchivo, doc.RutaFisica, doc.Observaciones); // CAMBIO: Usar RutaFisica
            }

            // Crear DataTable para documentos del Caso (TVP)
            var documentosCasoTable = new DataTable();
            documentosCasoTable.Columns.Add("tipo_documento_id", typeof(int));
            documentosCasoTable.Columns.Add("nombre_archivo", typeof(string));
            documentosCasoTable.Columns.Add("ruta_fisica", typeof(string)); // CAMBIO: Columna para la ruta
            documentosCasoTable.Columns.Add("observaciones", typeof(string));

            foreach (var doc in dto.DocumentosCaso)
            {
                documentosCasoTable.Rows.Add(doc.TipoDocumentoId, doc.NombreArchivo, doc.RutaFisica, doc.Observaciones); // CAMBIO: Usar RutaFisica
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
            parameters.Add("@numero_avaluo", dto.NumeroAvaluo);
            parameters.Add("@numero_reclamo", dto.NumeroReclamo);
            parameters.Add("@fecha_siniestro", dto.FechaSiniestro);
            parameters.Add("@caso_estado_id", dto.CasoEstadoId);

            // Parámetros de Documentos (TVP)
            // Asegúrate de que los nombres de los tipos de tabla coincidan con los de tu BD (dbo.DocumentoAseguradoTableType)
            parameters.Add("@documentos_asegurado", documentosAseguradoTable.AsTableValuedParameter("dbo.DocumentoAseguradoTableType"));
            parameters.Add("@documentos_caso", documentosCasoTable.AsTableValuedParameter("dbo.DocumentoCasoTableType"));

            // Parámetro de Auditoría
            parameters.Add("@usuario_id", usuarioId); // Se toma del parámetro del método

            // Parámetros de Salida del SP
            parameters.Add("@resultado", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@nuevo_caso_id", dbType: DbType.Int32, direction: ParameterDirection.Output);

            try
            {
                if (_db.State == ConnectionState.Closed)
                {
                    _db.Open();
                }

                _logger.LogInformation("Ejecutando SP '{SPName}' para crear caso con avalúo: {NumeroAvaluo}", storedProcedure, dto.NumeroAvaluo);

                await _db.ExecuteAsync(storedProcedure, parameters, commandType: CommandType.StoredProcedure);

                // Obtener los valores de los parámetros de salida
                var resultadoSp = parameters.Get<int>("@resultado");
                var nuevoCasoId = parameters.Get<int>("@nuevo_caso_id");

                _logger.LogInformation("SP '{SPName}' ejecutado. Resultado: {Resultado}, Nuevo Caso ID: {NuevoCasoId}", storedProcedure, resultadoSp, nuevoCasoId);

                // Mapear los códigos de resultado del SP a excepciones específicas
                switch (resultadoSp)
                {
                    case 0:
                        if (nuevoCasoId <= 0)
                        {
                            _logger.LogError("SP '{SPName}' devolvió resultado 0 (éxito) pero nuevo_caso_id es inválido: {NuevoCasoId}", storedProcedure, nuevoCasoId);
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
                    case -30:
                        throw new NumeroAvaluoCasoDuplicadoException($"El número de avalúo '{dto.NumeroAvaluo}' ya existe para otro caso.");
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
                _logger.LogError(ex, "Error de SQL al intentar crear el caso con avalúo '{NumeroAvaluo}'. Mensaje: {Message}", dto.NumeroAvaluo, ex.Message);
                throw new Exception($"Error de base de datos al crear el caso: {ex.Message}", ex);
            }
            catch (CasoCreationException)
            {
                throw; // Relanzar nuestras excepciones específicas
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al intentar crear el caso con avalúo '{NumeroAvaluo}'. Mensaje: {Message}", dto.NumeroAvaluo, ex.Message);
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
        /// Retrieves the full details of a case by its ID.
        /// </summary>
        /// <param name="casoId">The ID of the case to retrieve.</param>
        /// <returns>A CasoDetalleDto object if the case is found.</returns>
        /// <exception cref="CasoNoEncontradoException">Thrown if the case does not exist.</exception>
        /// <exception cref="CasoServiceException">Thrown for other unexpected errors.</exception>
        public async Task<CasoDetalleDto> ObtenerCasoPorIdAsync(string casoId) // Assuming caso_id is string like "001"
        {
            const string storedProcedure = "sp_ObtenerCasoPorId";

            var parameters = new DynamicParameters();
            parameters.Add("@caso_id", casoId, DbType.String, ParameterDirection.Input, 50); // Adjust DbType and size as per your DB schema
            parameters.Add("@resultado", dbType: DbType.Int32, direction: ParameterDirection.Output); // Output parameter from SP

            try
            {
                // Ensure the database connection is open.
                // Dapper often automatically opens the connection if needed,
                // but it's good practice to ensure it if multiple operations are performed.
                if (_db.State == ConnectionState.Closed)
                {
                    _db.Open();
                }

                _logger.LogInformation("Executing SP '{SPName}' to get case ID: {CasoId}", storedProcedure, casoId);

                // Execute the stored procedure and map the result to CasoDetalleDto.
                // QueryFirstOrDefaultAsync is suitable as the SP is expected to return a single row or null.
                var caso = await _db.QueryFirstOrDefaultAsync<CasoDetalleDto>(
                    storedProcedure,
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                // Retrieve the value of the output parameter
                int resultadoSp = parameters.Get<int>("@resultado");

                // Handle results based on the SP's output parameter
                if (resultadoSp == -1)
                {
                    _logger.LogWarning("Case with ID {CasoId} not found (SP result: -1).", casoId);
                    throw new CasoNoEncontradoException($"Case with ID {casoId} not found.");
                }
                else if (resultadoSp == -99)
                {
                    _logger.LogError("Internal SP error '{SPName}' when getting case ID: {CasoId}.", storedProcedure, casoId);
                    throw new CasoServiceException($"An internal server error occurred while retrieving the case.");
                }

                // If resultadoSp is 0 (OK) but caso is null, it means the SP returned 0 rows unexpectedly.
                if (caso == null)
                {
                    _logger.LogWarning("Case with ID {CasoId} not found (SP returned 0 rows, but result was 0).", casoId);
                    throw new CasoNoEncontradoException($"Case with ID {casoId} not found.");
                }

                _logger.LogInformation("Case with ID {CasoId} retrieved successfully.", casoId);
                return caso;
            }
            catch (SqlException ex) // Catch specific SQL Server exceptions
            {
                _logger.LogError(ex, "SQL error when retrieving case ID {CasoId} from SP '{SPName}'. Message: {Message}", casoId, storedProcedure, ex.Message);
                throw new CasoServiceException($"Database error when retrieving case: {ex.Message}", ex);
            }
            catch (CasoNoEncontradoException)
            {
                throw; // Re-throw the specific exception
            }
            catch (CasoServiceException)
            {
                throw; // Re-throw the specific exception
            }
            catch (Exception ex) // Catch any other unexpected exceptions
            {
                _logger.LogError(ex, "Unexpected error when retrieving case ID {CasoId} from SP '{SPName}'. Message: {Message}", casoId, storedProcedure, ex.Message);
                throw new CasoServiceException($"An unexpected error occurred while retrieving the case: {ex.Message}", ex);
            }
            finally
            {
                // It's good practice to close the connection if it was explicitly opened here,
                // or if the IDbConnection lifecycle is not managed by DI to keep it open.
                if (_db.State == ConnectionState.Open)
                {
                    _db.Close();
                }
            }
        }

    }
}
