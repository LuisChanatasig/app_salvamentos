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
            // 1) Configurar la conexión y capturar los PRINT del SP
            using var connection = new SqlConnection(_dbContext.Database.GetConnectionString());
            connection.InfoMessage += (sender, e) =>
            {
                // Cada línea de PRINT viene en e.Message
                _logger.LogInformation("SP INFO: {Message}", e.Message.Trim());
            };

            using var command = new SqlCommand("sp_RegistrarDatosCasoFinanciero_V3", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            // 2) Helper para construir un DataTable de documentos
            DataTable BuildDocTable(IEnumerable<DocumentoDto> docs)
            {
                var table = new DataTable();
                table.Columns.Add("tipo_documento_id", typeof(int));
                table.Columns.Add("nombre_archivo", typeof(string));
                table.Columns.Add("ruta_fisica", typeof(string));
                table.Columns.Add("observaciones", typeof(string));
                foreach (var doc in docs)
                {
                    if (string.IsNullOrWhiteSpace(doc.NombreArchivo)) continue;
                    table.Rows.Add(
                        doc.TipoDocumentoId,
                        doc.NombreArchivo,
                        doc.RutaFisica ?? string.Empty,
                        doc.Observaciones ?? string.Empty
                    );
                }
                return table;
            }

            // 3) Preparar los DataTables para cada TVP de documentos
            var tblCasos = BuildDocTable(datos.DocumentosCaso);
            var tblAsegurado = BuildDocTable(datos.DocumentosAsegurado);
            var tblValorComer = BuildDocTable(datos.DocumentosValorComercial);
            var tblDano = BuildDocTable(datos.DocumentosDano);

            _logger.LogInformation("TVP DocumentosValorComercial filas: {Count}", tblValorComer.Rows.Count);

            // 4) Preparar los DataTables para los TVPs de datos principales
            var tblValores = ToValoresComercialesTVP(datos.ValoresComerciales);
            var tblDanos = ToDanosTVP(datos.Danos);
            var tblPartes = ToPartesTVP(datos.Partes);

            // 5) Parámetros OUTPUT / INPUTOUTPUT
            var pCaso = new SqlParameter("@caso_id", SqlDbType.Int)
            {
                Direction = ParameterDirection.InputOutput,
                Value = datos.CasoId
            };
            command.Parameters.Add(pCaso);

            var pAseg = new SqlParameter("@asegurado_id", SqlDbType.Int)
            {
                Direction = ParameterDirection.InputOutput,
                Value = datos.AseguradoId
            };
            command.Parameters.Add(pAseg);

            var pVeh = new SqlParameter("@vehiculo_id", SqlDbType.Int)
            {
                Direction = ParameterDirection.InputOutput,
                Value = datos.VehiculoId
            };
            command.Parameters.Add(pVeh);

            // 6) Parámetros del caso y asegurado
            command.Parameters.AddWithValue("@usuario_id", datos.UsuarioId);
            command.Parameters.AddWithValue("@fecha_siniestro", datos.FechaSiniestro);
            command.Parameters.AddWithValue("@metodo_avaluo", (object?)datos.MetodoAvaluo ?? DBNull.Value);
            command.Parameters.AddWithValue("@direccion_avaluo", (object?)datos.DireccionAvaluo ?? DBNull.Value);
            command.Parameters.AddWithValue("@fecha_solicitud_avaluo", (object?)datos.FechaSolicitudAvaluo ?? DBNull.Value);
            command.Parameters.AddWithValue("@comentarios_avaluo", (object?)datos.ComentariosAvaluo ?? DBNull.Value);
            command.Parameters.AddWithValue("@notas_avaluo", (object?)datos.NotasAvaluo ?? DBNull.Value);
            command.Parameters.AddWithValue("@asegurado_nombre", datos.NombreCompleto);

            // 7) Parámetros del vehículo
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

            // 8) Parámetros de los TVPs de documentos
            command.Parameters.Add(new SqlParameter("@DocumentosCasoTVP", SqlDbType.Structured)
            {
                TypeName = "dbo.DocumentoCasoTableType",
                Value = tblCasos
            });
            command.Parameters.Add(new SqlParameter("@DocumentosAseguradoTVP", SqlDbType.Structured)
            {
                TypeName = "dbo.DocumentoAseguradoTableType",
                Value = tblAsegurado
            });
            command.Parameters.Add(new SqlParameter("@DocumentosValorComercialTVP", SqlDbType.Structured)
            {
                TypeName = "dbo.DocumentoValorComercialTableType",
                Value = tblValorComer
            });
            command.Parameters.Add(new SqlParameter("@DocumentosDanoTVP", SqlDbType.Structured)
            {
                TypeName = "dbo.DocumentoDanoTableType",
                Value = tblDano
            });

            // 9) Parámetros de los TVPs de datos principales
            command.Parameters.Add(new SqlParameter("@ValoresComercialesTVP", SqlDbType.Structured)
            {
                TypeName = "dbo.TVP_ValoresComerciales",
                Value = tblValores
            });
            command.Parameters.Add(new SqlParameter("@DanosTVP", SqlDbType.Structured)
            {
                TypeName = "dbo.TVP_Danos",
                Value = tblDanos
            });
            command.Parameters.Add(new SqlParameter("@PartesTVP", SqlDbType.Structured)
            {
                TypeName = "dbo.TVP_Partes",
                Value = tblPartes
            });

            // 10) Parámetros del resumen financiero
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

            // 11) Parámetros de control de guardado
            command.Parameters.AddWithValue("@es_guardado_parcial", datos.EsGuardadoParcial);
            command.Parameters.AddWithValue("@tab_actual", datos.TabActual);

            // 12) Ejecutar y recorrer los resultados
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            // Avanzamos hasta el result-set donde aparezca la columna "tipo_documento_id"
            while (true)
            {
                if (reader.FieldCount > 0 &&
                    reader.GetSchemaTable().Rows.Cast<DataRow>()
                          .Any(rw => rw["ColumnName"]!.ToString() == "tipo_documento_id"))
                {
                    _logger.LogInformation("----- Contenido DocumentosValorComercialTVP -----");
                    while (await reader.ReadAsync())
                    {
                        _logger.LogInformation(
                            "TVP Row → tipo={Tipo}, archivo={Archivo}, ruta={Ruta}, obs={Obs}",
                            reader["tipo_documento_id"],
                            reader["nombre_archivo"],
                            reader["ruta_fisica"],
                            reader["observaciones"]
                        );
                    }
                    break;
                }
                if (!await reader.NextResultAsync()) break;
            }

            // Consumir cualquier resto (mensajes PRINT o el SELECT final)
            do { } while (await reader.NextResultAsync());

            // 13) Capturar los OUTPUT
            datos.CasoId = (int)pCaso.Value;
            datos.AseguradoId = (int)pAseg.Value;
            datos.VehiculoId = (int)pVeh.Value;

            _logger.LogInformation(
                "Datos guardados. CasoId={CasoId}, AseguradoId={AseguradoId}, VehiculoId={VehiculoId}",
                datos.CasoId, datos.AseguradoId, datos.VehiculoId
            );
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
