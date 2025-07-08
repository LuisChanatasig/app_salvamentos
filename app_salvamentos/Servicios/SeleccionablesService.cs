using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient; // Para SqlException
using app_salvamentos.Models;
namespace app_salvamentos.Servicios
{
   
    /// <summary>
    /// Excepción personalizada para errores al listar perfiles.
    /// </summary>
    public class ListarPerfilesException : Exception
    {
        public ListarPerfilesException(string message) : base(message) { }
        public ListarPerfilesException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Excepción personalizada para errores al listar estados de caso.
    /// </summary>
    public class ListarEstadosCasoException : Exception
    {
        public ListarEstadosCasoException(string message) : base(message) { }
        public ListarEstadosCasoException(string message, Exception innerException) : base(message, innerException) { }
    }


    /// <summary>
    /// Servicio para operaciones relacionadas con listas seleccionables, como perfiles.
    /// </summary>
    public class SeleccionablesService
    {
        private readonly IDbConnection _db;
        private readonly ILogger<SeleccionablesService> _logger;
        /// <summary>
        /// Excepción personalizada para errores al listar tipos de documento.
        /// </summary>
        public class ListarTiposDocumentoException : Exception
        {
            public ListarTiposDocumentoException(string message) : base(message) { }
            public ListarTiposDocumentoException(string message, Exception innerException) : base(message, innerException) { }
        }

        public SeleccionablesService(IDbConnection db, ILogger<SeleccionablesService> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Lista los perfiles disponibles en el sistema.
        /// </summary>
        /// <param name="incluirInactivos">Indica si se deben incluir perfiles inactivos. Por defecto es false (solo activos).</param>
        /// <returns>Una lista de objetos PerfilDto.</returns>
        /// <exception cref="ListarPerfilesException">Lanzada si ocurre un error al listar los perfiles.</exception>
        public async Task<IEnumerable<PerfilDto>> ListarPerfilesAsync(bool incluirInactivos = false)
        {
            const string storedProcedure = "sp_ListarPerfiles";

            var parameters = new DynamicParameters();
            parameters.Add("@incluir_inactivos", incluirInactivos, DbType.Boolean);

            try
            {
                if (_db.State == ConnectionState.Closed)
                {
                    _db.Open();
                }

                _logger.LogInformation("Ejecutando SP '{SPName}' con incluirInactivos: {IncluirInactivos}", storedProcedure, incluirInactivos);

                // Ejecutar el Stored Procedure y mapear los resultados a una lista de PerfilDto
                var perfiles = await _db.QueryAsync<PerfilDto>(
                    storedProcedure,
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                _logger.LogInformation("SP '{SPName}' ejecutado exitosamente. Se encontraron {Count} perfiles.", storedProcedure, ((List<PerfilDto>)perfiles).Count);
                return perfiles;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error de SQL al intentar listar perfiles desde el SP '{SPName}'. Mensaje: {Message}", storedProcedure, ex.Message);
                throw new ListarPerfilesException($"Error de base de datos al listar perfiles: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al intentar listar perfiles desde el SP '{SPName}'. Mensaje: {Message}", storedProcedure, ex.Message);
                throw new ListarPerfilesException($"Ocurrió un error inesperado al listar perfiles: {ex.Message}", ex);
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
        /// Lista los tipos de documento disponibles en el sistema, con opciones de filtrado.
        /// </summary>
        /// <param name="soloActivos">Indica si se deben listar solo los tipos de documento activos. Por defecto es true.</param>
        /// <param name="ambito">Filtra los tipos de documento por un ámbito específico (ej. 'ASEGURADO', 'CASO', 'GENERAL'). Null para no filtrar.</param>
        /// <returns>Una lista de objetos TipoDocumentoDto.</returns>
        /// <exception cref="ListarTiposDocumentoException">Lanzada si ocurre un error al listar los tipos de documento.</exception>
        public async Task<IEnumerable<TipoDocumentoDto>> ListarTiposDocumentoAsync(bool soloActivos = true, string ambito = null)
        {
            const string storedProcedure = "sp_ListarTiposDocumento";

            var parameters = new DynamicParameters();
            parameters.Add("@solo_activos", soloActivos, DbType.Boolean);
            parameters.Add("@ambito", ambito, DbType.String, ParameterDirection.Input, 20); // Tamaño NVARCHAR(20)
            parameters.Add("@resultado", dbType: DbType.Int32, direction: ParameterDirection.Output); // Parámetro de salida

            try
            {
                if (_db.State == ConnectionState.Closed)
                {
                    _db.Open();
                }

                _logger.LogInformation("Ejecutando SP '{SPName}' con soloActivos: {SoloActivos}, ambito: {Ambito}", storedProcedure, soloActivos, ambito ?? "N/A");

                // Ejecutar el Stored Procedure
                var tiposDocumento = await _db.QueryAsync<TipoDocumentoDto>(
                    storedProcedure,
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                // Recuperar el valor del parámetro de salida
                var resultadoSp = parameters.Get<int>("@resultado");

                if (resultadoSp < 0)
                {
                    // Si el SP devuelve un error, lanzar una excepción específica
                    _logger.LogError("SP '{SPName}' devolvió un error al listar tipos de documento. Código: {Resultado}", storedProcedure, resultadoSp);
                    throw new ListarTiposDocumentoException($"Error al listar tipos de documento desde la base de datos. Código SP: {resultadoSp}");
                }

                _logger.LogInformation("SP '{SPName}' ejecutado exitosamente. Se encontraron {Count} tipos de documento.", storedProcedure, ((List<TipoDocumentoDto>)tiposDocumento).Count);
                return tiposDocumento;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error de SQL al intentar listar tipos de documento desde el SP '{SPName}'. Mensaje: {Message}", storedProcedure, ex.Message);
                throw new ListarTiposDocumentoException($"Error de base de datos al listar tipos de documento: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al intentar listar tipos de documento desde el SP '{SPName}'. Mensaje: {Message}", storedProcedure, ex.Message);
                throw new ListarTiposDocumentoException($"Ocurrió un error inesperado al listar tipos de documento: {ex.Message}", ex);
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
        /// Lista los estados de caso disponibles en el sistema.
        /// </summary>
        /// <param name="soloActivos">Indica si se deben listar solo los estados activos. Por defecto es true.</param>
        /// <returns>Una lista de objetos EstadoCasoDto.</returns>
        /// <exception cref="ListarEstadosCasoException">Lanzada si ocurre un error al listar los estados de caso.</exception>
        public async Task<IEnumerable<EstadoCasoDto>> ListarEstadosCasoAsync(bool soloActivos = true)
        {
            const string storedProcedure = "sp_ListarEstadosCaso";

            var parameters = new DynamicParameters();
            parameters.Add("@solo_activos", soloActivos, DbType.Boolean);
            parameters.Add("@resultado", dbType: DbType.Int32, direction: ParameterDirection.Output); // Parámetro de salida

            try
            {
                if (_db.State == ConnectionState.Closed)
                {
                    _db.Open();
                }

                _logger.LogInformation("Ejecutando SP '{SPName}' con soloActivos: {SoloActivos}", storedProcedure, soloActivos);

                // Ejecutar el Stored Procedure
                var estadosCaso = await _db.QueryAsync<EstadoCasoDto>(
                    storedProcedure,
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                // Recuperar el valor del parámetro de salida
                var resultadoSp = parameters.Get<int>("@resultado");

                if (resultadoSp < 0)
                {
                    // Si el SP devuelve un error, lanzar una excepción específica
                    _logger.LogError("SP '{SPName}' devolvió un error al listar estados de caso. Código: {Resultado}", storedProcedure, resultadoSp);
                    throw new ListarEstadosCasoException($"Error al listar estados de caso desde la base de datos. Código SP: {resultadoSp}");
                }

                _logger.LogInformation("SP '{SPName}' ejecutado exitosamente. Se encontraron {Count} estados de caso.", storedProcedure, ((List<EstadoCasoDto>)estadosCaso).Count);
                return estadosCaso;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error de SQL al intentar listar estados de caso desde el SP '{SPName}'. Mensaje: {Message}", storedProcedure, ex.Message);
                throw new ListarEstadosCasoException($"Error de base de datos al listar estados de caso: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al intentar listar estados de caso desde el SP '{SPName}'. Mensaje: {Message}", storedProcedure, ex.Message);
                throw new ListarEstadosCasoException($"Ocurrió un error inesperado al listar estados de caso: {ex.Message}", ex);
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
        /// Lista los tipos de documento filtrados por ámbito.
        /// </summary>
        /// <param name="ambito">El ámbito del documento (ej. 'CASO', 'ASEGURADO', 'GENERAL').</param>
        /// <returns>Una colección de TipoDocumentoDto.</returns>
        /// <exception cref="Exception">Lanzada si ocurre un error al listar los tipos de documento.</exception>
        public async Task<IEnumerable<TipoDocumentoDto>> ListarTiposDocumentoPorAmbitoAsync(string ambito)
        {
            const string storedProcedure = "sp_ListarTiposDocumentoPorAmbito"; // Nuevo SP necesario

            var parameters = new DynamicParameters();
            parameters.Add("@ambito_documento", ambito, DbType.String, ParameterDirection.Input, 20);

            try
            {
                if (_db.State == ConnectionState.Closed)
                {
                    _db.Open();
                }

                _logger.LogInformation("Ejecutando SP '{SPName}' para listar tipos de documento por ámbito: {Ambito}", storedProcedure, ambito);

                var tiposDocumento = await _db.QueryAsync<TipoDocumentoDto>(
                    storedProcedure,
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                _logger.LogInformation("SP '{SPName}' ejecutado exitosamente. Se encontraron {Count} tipos de documento para el ámbito {Ambito}.",
                    storedProcedure, tiposDocumento.Count(), ambito);

                return tiposDocumento;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error de SQL al listar tipos de documento por ámbito '{Ambito}' desde el SP '{SPName}'. Mensaje: {Message}", ambito, storedProcedure, ex.Message);
                throw new Exception($"Error de base de datos al listar tipos de documento: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al listar tipos de documento por ámbito '{Ambito}' desde el SP '{SPName}'. Mensaje: {Message}", ambito, storedProcedure, ex.Message);
                throw new Exception($"Ocurrió un error inesperado al listar tipos de documento: {ex.Message}", ex);
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
