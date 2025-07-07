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
    /// Servicio para operaciones relacionadas con listas seleccionables, como perfiles.
    /// </summary>
    public class SeleccionablesService
    {
        private readonly IDbConnection _db;
        private readonly ILogger<SeleccionablesService> _logger;

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
    }
}
