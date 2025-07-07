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
        /// <returns>El ID del caso recién creado.</returns>
        /// <exception cref="CasoCreationException">Lanzada para errores específicos durante la creación del caso.</exception>
        /// <exception cref="Exception">Lanzada para errores inesperados.</exception>
        public async Task<int> CrearCasoCompletoAsync(CrearCasoDto dto)
        {
            const string storedProcedure = "sp_CrearCasoCompleto";

            // Crear DataTable para documentos del Asegurado (TVP)
            var documentosAseguradoTable = new DataTable();
            documentosAseguradoTable.Columns.Add("tipo_documento_id", typeof(int));
            documentosAseguradoTable.Columns.Add("nombre_archivo", typeof(string));
            documentosAseguradoTable.Columns.Add("contenido_binario", typeof(byte[]));
            documentosAseguradoTable.Columns.Add("observaciones", typeof(string));

            foreach (var doc in dto.DocumentosAsegurado)
            {
                documentosAseguradoTable.Rows.Add(doc.TipoDocumentoId, doc.NombreArchivo, doc.ContenidoBinario, doc.Observaciones);
            }

            // Crear DataTable para documentos del Caso (TVP)
            var documentosCasoTable = new DataTable();
            documentosCasoTable.Columns.Add("tipo_documento_id", typeof(int));
            documentosCasoTable.Columns.Add("nombre_archivo", typeof(string));
            documentosCasoTable.Columns.Add("contenido_binario", typeof(byte[]));
            documentosCasoTable.Columns.Add("observaciones", typeof(string));

            foreach (var doc in dto.DocumentosCaso)
            {
                documentosCasoTable.Rows.Add(doc.TipoDocumentoId, doc.NombreArchivo, doc.ContenidoBinario, doc.Observaciones);
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
            parameters.Add("@usuario_id", dto.UsuarioId);

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
                // Aquí podrías intentar parsear el mensaje de error de SQL para dar una excepción más específica
                // si el SP no devuelve un resultado numérico para todos los casos de error.
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


    }
}
