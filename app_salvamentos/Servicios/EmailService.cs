using app_salvamentos.Configuration;
using System.Net.Mail;
using System.Net;
using System.Text;

namespace app_salvamentos.Servicios
{
    public class EmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly CasosService _casoService; // Inyecta tu servicio de casos
        // Necesitarás también las settings de tu almacenamiento para construir rutas físicas
        private readonly FileStorageSettings _fileStorageSettings;

        // Constructor para inyectar las dependencias
        public EmailService(
            ILogger<EmailService> logger,
            CasosService casoService,
            Microsoft.Extensions.Options.IOptions<FileStorageSettings> fileStorageSettings) // Inyecta IOptions
        {
            _logger = logger;
            _casoService = casoService;
            _fileStorageSettings = fileStorageSettings.Value;
        }
        // Nuevo DTO para el resultado de la preparación del email
public class EmailPrepareResultDto
{
    public string MailtoUri { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string PlainTextBody { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public List<string> AttachmentPathsNotSent { get; set; } = new List<string>(); // Rutas de archivos que el usuario debe adjuntar manualmente
}
        /// <summary>
        /// Prepara el contenido de un email de avalúo para ser abierto en un cliente de correo externo.
        /// No envía el email directamente, sino que devuelve la información necesaria para un enlace mailto:.
        /// </summary>
        /// <param name="casoId">El ID del caso.</param>
        /// <param name="numeroReclamo">El número de reclamo asociado al caso.</param>
        /// <returns>Un EmailPrepareResultDto con el URI mailto:, el cuerpo HTML y los archivos que no se pueden adjuntar.</returns>
        /// <exception cref="InvalidOperationException">Lanzada si el caso no se encuentra.</exception>
        public async Task<EmailPrepareResultDto> PrepareAvaluoEmailAsync(int casoId, string numeroReclamo)
        {
            _logger.LogInformation("Preparando contenido de email para avalúo del caso {CasoId} ({NumeroReclamo})...", casoId, numeroReclamo);

            try
            {
                // 1. Obtener datos completos del caso y el análisis/avalúo
                var casoDetalle = await _casoService.ObtenerCasoPorIdAsync(casoId);

                if (casoDetalle == null)
                {
                    _logger.LogWarning("No se encontró el detalle del caso {CasoId} para preparar el avalúo.", casoId);
                    throw new InvalidOperationException($"No se encontró el caso con ID {casoId} para preparar el avalúo.");
                }

                string? destinatarioEmail = casoDetalle.email;
                if (string.IsNullOrWhiteSpace(destinatarioEmail))
                {
                    _logger.LogWarning("El caso {CasoId} no tiene un email de destinatario configurado para el asegurado. Se asignará un correo genérico.", casoDetalle.caso_id);
                    destinatarioEmail = "fchanatasig@if2bpo.com"; // Asigna el correo genérico
                }

                string subject = $"Avalúo Finalizado - Reclamo: {casoDetalle.numero_reclamo}";

                // 2. Construir el contenido del email (HTML)
                var bodyHtmlBuilder = new StringBuilder();
                bodyHtmlBuilder.Append($@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ width: 80%; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px; }}
                    h1 {{ color: #0056b3; }}
                    h2 {{ color: #0056b3; border-bottom: 1px solid #eee; padding-bottom: 5px; margin-top: 20px; }}
                    p {{ margin-bottom: 10px; }}
                    .data-row {{ margin-bottom: 5px; }}
                    .label {{ font-weight: bold; display: inline-block; width: 180px; }} /* Ajustado para alineación */
                    .value {{ display: inline-block; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h1>Estimado/a {casoDetalle.nombre_asegurado ?? "Cliente"},</h1>
                    <p>Nos complace informarle que el avalúo para su reclamo ha sido finalizado y está listo para su revisión.</p>
                    <p><strong>Número de Reclamo:</strong> {casoDetalle.numero_reclamo}</p>
                    <p><strong>Número de Avalúo:</strong> {casoDetalle.numero_avaluo}</p>
                    <p><strong>Fecha de Siniestro:</strong> {casoDetalle.fecha_siniestro?.ToString("dd/MM/yyyy")}</p>

                    <h2>Detalles del Vehículo</h2>
                    <div class='data-row'><span class='label'>Marca:</span> <span class='value'>{casoDetalle.marca}</span></div>
                    <div class='data-row'><span class='label'>Modelo:</span> <span class='value'>{casoDetalle.modelo}</span></div>
                    <div class='data-row'><span class='label'>Placa:</span> <span class='value'>{casoDetalle.placa}</span></div>
                    <div class='data-row'><span class='label'>Año:</span> <span class='value'>{casoDetalle.anio}</span></div>
                    <div class='data-row'><span class='label'>Chasis:</span> <span class='value'>{casoDetalle.numero_chasis}</span></div>
                    <div class='data-row'><span class='label'>Motor:</span> <span class='value'>{casoDetalle.numero_motor}</span></div>
                    <div class='data-row'><span class='label'>Tipo Vehículo:</span> <span class='value'>{casoDetalle.tipo_vehiculo}</span></div>
                    <div class='data-row'><span class='label'>Clase:</span> <span class='value'>{casoDetalle.clase}</span></div>
                    <div class='data-row'><span class='label'>Color:</span> <span class='value'>{casoDetalle.color}</span></div>
                    <div class='data-row'><span class='label'>Transmisión:</span> <span class='value'>{casoDetalle.transmision}</span></div>
                    <div class='data-row'><span class='label'>Combustible:</span> <span class='value'>{casoDetalle.combustible}</span></div>
                    <div class='data-row'><span class='label'>Cilindraje:</span> <span class='value'>{casoDetalle.cilindraje}</span></div>
                    <div class='data-row'><span class='label'>Gravamen:</span> <span class='value'>{casoDetalle.gravamen ?? "N/A"}</span></div>
                    <div class='data-row'><span class='label'>Placas Metálicas:</span> <span class='value'>{casoDetalle.placas_metalicas ?? "N/A"}</span></div>
                    <div class='data-row'><span class='label'>Radio en Vehículo:</span> <span class='value'>{casoDetalle.radio_vehiculo ?? "N/A"}</span></div>
                    <div class='data-row'><span class='label'>Estado Vehículo:</span> <span class='value'>{casoDetalle.estado_vehiculo ?? "N/A"}</span></div>
                    <div class='data-row'><span class='label'>Observaciones Vehículo:</span> <span class='value'>{casoDetalle.observaciones_vehiculo ?? "Sin observaciones"}</span></div>

                    <h2>Resumen del Análisis Financiero</h2>");

                if (casoDetalle.Resumen != null)
                {
                    bodyHtmlBuilder.Append($@"
                    <div class='data-row'><span class='label'>Valor Matricula Pendiente:</span> <span class='value'>{casoDetalle.Resumen.ValorMatriculaPendiente:C}</span></div>
                    <div class='data-row'><span class='label'>Fecha Límite Pago SRI:</span> <span class='value'>{casoDetalle.Resumen.FechaLimitePagoSri.ToString("dd/MM/yyyy")}</span></div>
                    <div class='data-row'><span class='label'>Número Multas:</span> <span class='value'>{casoDetalle.Resumen.NumeroMultas}</span></div>
                    <div class='data-row'><span class='label'>Valor Multas Total:</span> <span class='value'>{casoDetalle.Resumen.ValorMultasTotal:C}</span></div>
                    <div class='data-row'><span class='label'>Valor Asegurado:</span> <span class='value'>{casoDetalle.Resumen.ValorAsegurado:C}</span></div>
                    <div class='data-row'><span class='label'>Promedio Calculado:</span> <span class='value'>{casoDetalle.Resumen.PromedioCalculado:C}</span></div>
                    <div class='data-row'><span class='label'>Promedio Neto:</span> <span class='value'>{casoDetalle.Resumen.PromedioNeto:C}</span></div>
                    <div class='data-row'><span class='label'>Porcentaje Daño:</span> <span class='value'>{casoDetalle.Resumen.PorcentajeDano}%</span></div>
                    <div class='data-row'><span class='label'>Valor Salvamento:</span> <span class='value'>{casoDetalle.Resumen.ValorSalvamento:C}</span></div>
                    <div class='data-row'><span class='label'>Precio Comercial Sugerido:</span> <span class='value'>{casoDetalle.Resumen.PrecioComercialSugerido:C}</span></div>
                    <div class='data-row'><span class='label'>Precio Base:</span> <span class='value'>{casoDetalle.Resumen.PrecioBase:C}</span></div>
                    <div class='data-row'><span class='label'>Precio Estimado Venta Vehículo:</span> <span class='value'>{casoDetalle.Resumen.PrecioEstimadoVentaVehiculo:C}</span></div>
                ");
                }
                else
                {
                    bodyHtmlBuilder.Append("<p>No se registró resumen financiero.</p>");
                }

                bodyHtmlBuilder.Append("<h3>Valores Comerciales</h3>");
                if (casoDetalle.ValoresComerciales != null && casoDetalle.ValoresComerciales.Any())
                {
                    foreach (var vc in casoDetalle.ValoresComerciales)
                    {
                        bodyHtmlBuilder.Append($"<p class='data-row'><span class='label'>Fuente {vc.Fuente ?? "N/A"}:</span> <span class='value'>{vc.Valor:C}</span></p>");
                    }
                }
                else
                {
                    bodyHtmlBuilder.Append("<p>No se registraron valores comerciales.</p>");
                }

                bodyHtmlBuilder.Append("<h3>Daños Registrados</h3>");
                if (casoDetalle.Danos != null && casoDetalle.Danos.Any())
                {
                    foreach (var dano in casoDetalle.Danos)
                    {
                        bodyHtmlBuilder.Append($"<p class='data-row'><span class='label'>Observaciones Daño:</span> <span class='value'>{dano.Observaciones ?? "Sin observaciones"}</span></p>");
                    }
                }
                else
                {
                    bodyHtmlBuilder.Append("<p>No se registraron daños.</p>");
                }

                bodyHtmlBuilder.Append("<h3>Partes Reemplazadas</h3>");
                if (casoDetalle.Partes != null && casoDetalle.Partes.Any())
                {
                    foreach (var parte in casoDetalle.Partes)
                    {
                        bodyHtmlBuilder.Append($"<p class='data-row'><span class='label'>Nombre Parte:</span> <span class='value'>{parte.NombreParte ?? "N/A"}</span> - <span class='label'>Costo Nuevo:</span> <span class='value'>{parte.ValorNuevo:C}</span> - <span class='label'>Costo Depreciado:</span> <span class='value'>{parte.ValorDepreciado:C}</span></p>");
                    }
                }
                else
                {
                    bodyHtmlBuilder.Append("<p>No se registraron partes.</p>");
                }

                bodyHtmlBuilder.Append($@"
                    <p>Saludos cordiales,<br>Su Equipo de Gestión de Reclamos</p>
                </div>
            </body>
            </html>");

                string bodyHtml = bodyHtmlBuilder.ToString();
                string plainTextBody = ConvertHtmlToPlainText(bodyHtml); // Convertir a texto plano para mailto

                // 3. Recopilar archivos que NO se pueden adjuntar automáticamente
                var attachmentPathsNotSent = new List<string>();

                if (casoDetalle.DocumentosCaso != null)
                {
                    foreach (var doc in casoDetalle.DocumentosCaso)
                    {
                        // Asegúrate de que _fileStorageSettings.BaseUploadPath esté configurado
                        // y que ruta_fisica sea la parte relativa esperada.
                        string fullPath = Path.Combine(_fileStorageSettings.BaseUploadPath, doc.ruta_fisica?.Replace("/", "\\") ?? "");
                        if (System.IO.File.Exists(fullPath))
                        {
                            attachmentPathsNotSent.Add(fullPath); // Añadir a la lista de no adjuntados
                        }
                        else
                        {
                            _logger.LogWarning("Archivo adjunto no encontrado para informar al usuario: {Path}", fullPath);
                        }
                    }
                }
                // Puedes hacer lo mismo para otros tipos de documentos relevantes (ej. DocumentosValorComercial)

                // 4. Construir el URI mailto:
                // Limitar la longitud del cuerpo para mailto: ya que los clientes de correo tienen límites.
                // Algunos clientes de correo tienen límites de 2000-4000 caracteres para el URI completo.
                const int mailtoBodyLimit = 1500; // Un límite conservador para el cuerpo del mailto
                string truncatedPlainTextBody = plainTextBody.Length > mailtoBodyLimit ?
                                                plainTextBody.Substring(0, mailtoBodyLimit) + "..." :
                                                plainTextBody;

                // Usar WebUtility.UrlEncode para asegurar que el URI sea válido
                string mailtoUri = $"mailto:{WebUtility.UrlEncode(destinatarioEmail)}?subject={WebUtility.UrlEncode(subject)}&body={WebUtility.UrlEncode(truncatedPlainTextBody)}";

                _logger.LogInformation("Email para avalúo del caso {CasoId} ({NumeroReclamo}) preparado para envío a {Destinatario}.", casoId, numeroReclamo, destinatarioEmail);

                return new EmailPrepareResultDto
                {
                    MailtoUri = mailtoUri,
                    HtmlBody = bodyHtml,
                    PlainTextBody = plainTextBody,
                    Subject = subject,
                    RecipientEmail = destinatarioEmail,
                    AttachmentPathsNotSent = attachmentPathsNotSent
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrió un error inesperado al preparar el email para avalúo del caso {CasoId}.", casoId);
                throw new InvalidOperationException($"Error al preparar el email para el caso {casoId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Convierte HTML básico a texto plano.
        /// </summary>
        /// <param name="html">Cadena HTML a convertir.</param>
        /// <returns>Cadena de texto plano.</returns>
        private string ConvertHtmlToPlainText(string html)
        {
            // Un enfoque muy básico. Para una conversión robusta, considera una librería.
            string plainText = html;
            plainText = plainText.Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n");
            plainText = plainText.Replace("<p>", "\n\n").Replace("</p>", "");
            plainText = System.Text.RegularExpressions.Regex.Replace(plainText, "<[^>]*>", ""); // Elimina todas las etiquetas HTML
            plainText = WebUtility.HtmlDecode(plainText); // Decodifica entidades HTML como &amp;
            return plainText.Trim();
        }
    }
}
