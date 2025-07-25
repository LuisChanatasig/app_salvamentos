using app_salvamentos.Configuration;
using app_salvamentos.Models;
using DocumentFormat.OpenXml.InkML;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;

namespace app_salvamentos.Servicios
{
    public class AnalisisService
    {
      
        private readonly ILogger<CasosService> _logger;
        private readonly AppAutopiezasContext _dbContext;
        private readonly FileStorageSettings _fileStorageSettings;

        public AnalisisService( ILogger<CasosService> logger, AppAutopiezasContext dbContext, IOptions<FileStorageSettings> fileStorageOptions)
        {
            _dbContext = dbContext;
            _logger = logger;
          
            _fileStorageSettings = fileStorageOptions.Value; // ✅ ESTO AHORA FUNCIONA
        }



        public async Task RegistrarDatosCasoFinancieroAsync(DatosCasoFinancieroDto datos)
        {
            using var connection = new SqlConnection(_dbContext.Database.GetConnectionString());
            using var command = new SqlCommand("sp_RegistrarDatosCasoFinanciero_V3", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            // --- 1. Preparar TVPs para Documentos ---
            var documentosCasoTable = new DataTable();
            documentosCasoTable.Columns.Add("tipo_documento_id", typeof(int));
            documentosCasoTable.Columns.Add("nombre_archivo", typeof(string));
            documentosCasoTable.Columns.Add("ruta_fisica", typeof(string));
            documentosCasoTable.Columns.Add("observaciones", typeof(string));
            foreach (var doc in datos.DocumentosCaso)
            {
                documentosCasoTable.Rows.Add(doc.TipoDocumentoId, doc.NombreArchivo, doc.RutaFisica, doc.Observaciones);
            }

            var documentosAseguradoTable = new DataTable();
            documentosAseguradoTable.Columns.Add("tipo_documento_id", typeof(int));
            documentosAseguradoTable.Columns.Add("nombre_archivo", typeof(string));
            documentosAseguradoTable.Columns.Add("ruta_fisica", typeof(string));
            documentosAseguradoTable.Columns.Add("observaciones", typeof(string));
            foreach (var doc in datos.DocumentosAsegurado)
            {
                documentosAseguradoTable.Rows.Add(doc.TipoDocumentoId, doc.NombreArchivo, doc.RutaFisica, doc.Observaciones);
            }

            var documentosValorComercialTable = new DataTable();
            documentosValorComercialTable.Columns.Add("tipo_documento_id", typeof(int));
            documentosValorComercialTable.Columns.Add("nombre_archivo", typeof(string));
            documentosValorComercialTable.Columns.Add("ruta_fisica", typeof(string));
            documentosValorComercialTable.Columns.Add("observaciones", typeof(string));
            foreach (var doc in datos.DocumentosValorComercial)
            {
                documentosValorComercialTable.Rows.Add(doc.TipoDocumentoId, doc.NombreArchivo, doc.RutaFisica, doc.Observaciones);
            }

            var documentosDanoTable = new DataTable();
            documentosDanoTable.Columns.Add("tipo_documento_id", typeof(int));
            documentosDanoTable.Columns.Add("nombre_archivo", typeof(string));
            documentosDanoTable.Columns.Add("ruta_fisica", typeof(string));
            documentosDanoTable.Columns.Add("observaciones", typeof(string));
            foreach (var doc in datos.DocumentosDano)
            {
                documentosDanoTable.Rows.Add(doc.TipoDocumentoId, doc.NombreArchivo, doc.RutaFisica, doc.Observaciones);
            }

            // --- 2. Añadir Parámetros al Comando SQL ---

            // Parámetros OUTPUT/INPUTOUTPUT
            var casoIdParam = new SqlParameter("@caso_id", SqlDbType.Int)
            {
                Direction = ParameterDirection.InputOutput,
                Value = datos.CasoId // Pasa el valor existente (0 para nuevo, ID para actualizar)
            };
            command.Parameters.Add(casoIdParam);

            var aseguradoIdParam = new SqlParameter("@asegurado_id", SqlDbType.Int)
            {
                Direction = ParameterDirection.InputOutput,
                Value = datos.AseguradoId
            };
            command.Parameters.Add(aseguradoIdParam);

            var vehiculoIdParam = new SqlParameter("@vehiculo_id", SqlDbType.Int)
            {
                Direction = ParameterDirection.InputOutput,
                Value = datos.VehiculoId
            };
            command.Parameters.Add(vehiculoIdParam);

            // Resto de parámetros de entrada
            command.Parameters.AddWithValue("@usuario_id", datos.UsuarioId);
            command.Parameters.AddWithValue("@fecha_siniestro", datos.FechaSiniestro);
            command.Parameters.AddWithValue("@metodo_avaluo", (object?)datos.MetodoAvaluo ?? DBNull.Value);
            command.Parameters.AddWithValue("@direccion_avaluo", (object?)datos.DireccionAvaluo ?? DBNull.Value);
            command.Parameters.AddWithValue("@fecha_solicitud_avaluo", (object?)datos.FechaSolicitudAvaluo ?? DBNull.Value);
            command.Parameters.AddWithValue("@comentarios_avaluo", (object?)datos.ComentariosAvaluo ?? DBNull.Value);
            command.Parameters.AddWithValue("@notas_avaluo", (object?)datos.NotasAvaluo ?? DBNull.Value);
            command.Parameters.AddWithValue("@asegurado_nombre", datos.NombreCompleto);

            // Parámetros del Vehículo
            var v = datos.Vehiculo;
            command.Parameters.AddWithValue("@placa", (object?)v.Placa ?? DBNull.Value);
            command.Parameters.AddWithValue("@marca", (object?)v.Marca ?? DBNull.Value);
            command.Parameters.AddWithValue("@modelo", (object?)v.Modelo ?? DBNull.Value);
            command.Parameters.AddWithValue("@transmision", (object?)v.Transmision ?? DBNull.Value);
            command.Parameters.AddWithValue("@combustible", (object?)v.Combustible ?? DBNull.Value);
            command.Parameters.AddWithValue("@cilindraje", (object?)v.Cilindraje ?? DBNull.Value);
            command.Parameters.AddWithValue("@anio", v.Anio);
            command.Parameters.AddWithValue("@numero_chasis", (object?)v.NumeroChasis ?? DBNull.Value);
            command.Parameters.AddWithValue("@numero_motor", (object?)v.NumeroMotor ?? DBNull.Value);
            command.Parameters.AddWithValue("@tipo_vehiculo", (object?)v.TipoVehiculo ?? DBNull.Value);
            command.Parameters.AddWithValue("@clase", (object?)v.Clase ?? DBNull.Value);
            command.Parameters.AddWithValue("@color", (object?)v.Color ?? DBNull.Value);
            command.Parameters.AddWithValue("@observaciones", (object?)v.Observaciones ?? DBNull.Value);
            command.Parameters.AddWithValue("@gravamen", (object?)v.Gravamen ?? DBNull.Value);
            command.Parameters.AddWithValue("@placas_metalicas", (object?)v.PlacasMetalicas ?? DBNull.Value);
            command.Parameters.AddWithValue("@radio_vehiculo", (object?)v.RadioVehiculo ?? DBNull.Value);
            command.Parameters.AddWithValue("@estado_vehiculo", (object?)v.EstadoVehiculo ?? DBNull.Value);

            // Parámetros de TVP para las listas de documentos
            command.Parameters.Add(new SqlParameter("@DocumentosCasoTVP", SqlDbType.Structured)
            {
                TypeName = "dbo.DocumentoCasoTableType",
                Value = documentosCasoTable
            });
            command.Parameters.Add(new SqlParameter("@DocumentosAseguradoTVP", SqlDbType.Structured)
            {
                TypeName = "dbo.DocumentoAseguradoTableType",
                Value = documentosAseguradoTable
            });
            command.Parameters.Add(new SqlParameter("@DocumentosValorComercialTVP", SqlDbType.Structured)
            {
                TypeName = "dbo.DocumentoValorComercialTableType",
                Value = documentosValorComercialTable
            });
            command.Parameters.Add(new SqlParameter("@DocumentosDanoTVP", SqlDbType.Structured)
            {
                TypeName = "dbo.DocumentoDanoTableType",
                Value = documentosDanoTable
            });

            // Parámetros para ValorComercial y Daños y Partes (TVPs)
            command.Parameters.Add(new SqlParameter("@ValoresComercialesTVP", SqlDbType.Structured)
            {
                TypeName = "dbo.TVP_ValoresComerciales",
                Value = ToValoresComercialesTVP(datos.ValoresComerciales)
            });
            command.Parameters.Add(new SqlParameter("@DanosTVP", SqlDbType.Structured)
            {
                TypeName = "dbo.TVP_Danos",
                Value = ToDanosTVP(datos.Danos)
            });
            command.Parameters.Add(new SqlParameter("@PartesTVP", SqlDbType.Structured)
            {
                TypeName = "dbo.TVP_Partes",
                Value = ToPartesTVP(datos.Partes)
            });

            // Parámetros de Resumen Financiero
            var r = datos.Resumen;
            command.Parameters.AddWithValue("@fecha_limite_pago_sri", (object?)r.FechaLimitePagoSri ?? DBNull.Value);
            command.Parameters.AddWithValue("@numero_multas", (object?)r.NumeroMultas ?? DBNull.Value);
            command.Parameters.AddWithValue("@valor_multas_total", (object?)r.ValorMultasTotal ?? DBNull.Value);
            command.Parameters.AddWithValue("@valor_asegurado", (object?)r.ValorAsegurado ?? DBNull.Value);
            command.Parameters.AddWithValue("@valor_matricula_pendiente", (object?)r.ValorMatriculaPendiente ?? DBNull.Value);
            command.Parameters.AddWithValue("@promedio_calculado", (object?)r.PromedioCalculado ?? DBNull.Value);
            command.Parameters.AddWithValue("@promedio_neto", (object?)r.PromedioNeto ?? DBNull.Value);
            command.Parameters.AddWithValue("@porcentaje_dano", (object?)r.PorcentajeDano ?? DBNull.Value);
            command.Parameters.AddWithValue("@valor_salvamento", (object?)r.ValorSalvamento ?? DBNull.Value);
            command.Parameters.AddWithValue("@precio_comercial_sugerido", (object?)r.PrecioComercialSugerido ?? DBNull.Value);
            command.Parameters.AddWithValue("@precio_base", (object?)r.PrecioBase ?? DBNull.Value);
            command.Parameters.AddWithValue("@precio_estimado_venta_vehiculo", (object?)r.PrecioEstimadoVentaVehiculo ?? DBNull.Value);

            //// Parámetros opcionales para IDs de documentos relacionados
            //command.Parameters.AddWithValue("@ValorComercialDocId", (object?)datos.ValorComercialDocId ?? DBNull.Value);
            //command.Parameters.AddWithValue("@DanoDocId", (object?)datos.DanoDocId ?? DBNull.Value);

            try
            {
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();

                // --- 3. Capturar los valores OUTPUT después de la ejecución ---
                datos.CasoId = (int)casoIdParam.Value;
                datos.AseguradoId = (int)aseguradoIdParam.Value;
                datos.VehiculoId = (int)vehiculoIdParam.Value;

                _logger.LogInformation("Datos financieros y documentos registrados/actualizados exitosamente. IDs generados/actualizados: CasoId={CasoId}, AseguradoId={AseguradoId}, VehiculoId={VehiculoId}", datos.CasoId, datos.AseguradoId, datos.VehiculoId);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error SQL al registrar datos financieros para CasoId: {CasoId}. Mensaje: {Message}", datos.CasoId, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al registrar datos financieros para CasoId: {CasoId}. Mensaje: {Message}", datos.CasoId, ex.Message);
                throw;
            }
        }
        private static DataTable ToValoresComercialesTVP(List<ValorComercialDto> valores)
        {
            var table = new DataTable();
            table.Columns.Add("fuente", typeof(string));
            table.Columns.Add("valor", typeof(decimal));

            foreach (var item in valores)
            {
                table.Rows.Add(item.Fuente, item.Valor);
            }
            return table;
        }

        private static DataTable ToDanosTVP(List<DanoDto> danos)
        {
            var table = new DataTable();
            table.Columns.Add("observaciones", typeof(string));

            foreach (var item in danos)
            {
                table.Rows.Add((object?)item.Observaciones ?? DBNull.Value);
            }
            return table;
        }

        private static DataTable ToPartesTVP(List<ParteDto> partes)
        {
            var table = new DataTable();
            table.Columns.Add("nombre_parte", typeof(string));
            table.Columns.Add("valor_nuevo", typeof(decimal));
            table.Columns.Add("valor_depreciado", typeof(decimal));

            foreach (var item in partes)
            {
                table.Rows.Add(item.NombreParte, item.ValorNuevo, item.ValorDepreciado);
            }
            return table;
        }

        private static DataTable ToDocumentosTVP(List<DocumentoDto> documentos)
        {
            var table = new DataTable();
            table.Columns.Add("tipo_documento_id", typeof(int));
            table.Columns.Add("nombre_archivo", typeof(string));
            table.Columns.Add("ruta_fisica", typeof(string));
            table.Columns.Add("observaciones", typeof(string));

            foreach (var doc in documentos)
            {
                table.Rows.Add(doc.TipoDocumentoId, doc.NombreArchivo, doc.RutaFisica, (object?)doc.Observaciones ?? DBNull.Value);
            }
            return table;
        }
    }
}
