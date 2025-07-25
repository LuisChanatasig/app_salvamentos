namespace app_salvamentos.Models
{
    // Este es tu DTO actual, que se usa para persistir en la DB
    public class DatosCasoFinancieroDto // <--- Este es el "original" o "de persistencia"
    {
        public int CasoId { get; set; }
        public int UsuarioId { get; set; }
        public int AseguradoId { get; set; }
        public int VehiculoId { get; set; }

        public string? NombreCompleto { get; set; }
        public string? MetodoAvaluo { get; set; }
        public string? DireccionAvaluo { get; set; }
        public string? ComentariosAvaluo { get; set; }
        public string? NotasAvaluo { get; set; }
        public DateTime? FechaSiniestro { get; set; }
        public DateTime? FechaSolicitudAvaluo { get; set; }

        public VehiculoDto Vehiculo { get; set; }

        public List<ValorComercialDto> ValoresComerciales { get; set; }
        public List<DanoDto> Danos { get; set; }
        public List<ParteDto> Partes { get; set; }

        // Estas listas esperan DocumentoDto, que ya tienen RutaFisica
        public List<DocumentoDto> DocumentosCaso { get; set; }
        public List<DocumentoDto> DocumentosAsegurado { get; set; }
        public List<DocumentoDto> DocumentosValorComercial { get; set; }
        public List<DocumentoDto> DocumentosDano { get; set; }

        public ResumenFinancieroDto? Resumen { get; set; }
    }
}
