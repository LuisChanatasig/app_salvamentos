namespace app_salvamentos.Models
{
    public class ResumenFinancieroDto
    {
        public DateTime FechaLimitePagoSri { get; set; } = DateTime.Today;
        public string FechaLimitePagoSriFormatted => FechaLimitePagoSri.ToString("yyyy-MM-dd");
        public int NumeroMultas { get; set; }
        public decimal ValorMultasTotal { get; set; }
        public decimal ValorAsegurado { get; set; }
        public decimal ValorMatriculaPendiente { get; set; }
        public decimal PromedioCalculado { get; set; }
        public decimal PromedioNeto { get; set; }
        public decimal PorcentajeDano { get; set; }
        public decimal ValorSalvamento { get; set; }
        public decimal PrecioComercialSugerido { get; set; }
        public decimal PrecioBase { get; set; }
        public decimal PrecioEstimadoVentaVehiculo { get; set; }
    }
}
