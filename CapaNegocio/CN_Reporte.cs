using CapaEntidad;
using System;
using System.Collections.Generic;
using CapaDatos;

namespace CapaNegocio
{
    public class CN_Reporte
    {
        private readonly CD_Reporte _datos = new CD_Reporte();

        // Método que ya tienes para el resumen del Dashboard (compatibilidad)
        public Dictionary<string, int> VerDashboard()
        {
            return _datos.VerDashboard();
        }

        // Nuevo: devolver objeto fuertemente tipado
        public ReporteDashboard ObtenerReporteDashboard()
        {
            return _datos.ObtenerReporteDashboard();
        }

        // Acepta fechas nulas para que el controlador pueda pasar parámetros opcionales
        public List<ReporteVentas> ReporteVentas(DateTime? fechaInicio, DateTime? fechaFin, string idTransaccion)
        {
            return _datos.ReporteVentas(fechaInicio, fechaFin, idTransaccion);
        }
    }
}
