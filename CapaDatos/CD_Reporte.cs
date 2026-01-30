using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace CapaDatos
{
    public class CD_Reporte
    {
        // Método que ya tienes para el resumen del Dashboard (mantengo por compatibilidad)
        public Dictionary<string, int> VerDashboard()
        {
            var resumen = new Dictionary<string, int>
            {
                { "TotalClientes", 0 },
                { "TotalVentas", 0 },
                { "TotalProductos", 0 }
            };

            using (SqlConnection conn = new SqlConnection(Conexion.cn))
            using (SqlCommand cmd = new SqlCommand("sp_ReporteDashboard", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                conn.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        resumen["TotalClientes"] = dr["TotalClientes"] != DBNull.Value ? Convert.ToInt32(dr["TotalClientes"]) : 0;
                        resumen["TotalVentas"] = dr["TotalVentas"] != DBNull.Value ? Convert.ToInt32(dr["TotalVentas"]) : 0;
                        resumen["TotalProductos"] = dr["TotalProductos"] != DBNull.Value ? Convert.ToInt32(dr["TotalProductos"]) : 0;
                    }
                }
            }

            return resumen;
        }

        // Nuevo método: devuelve un objeto fuertemente tipado para el dashboard
        public ReporteDashboard ObtenerReporteDashboard()
        {
            var modelo = new ReporteDashboard();

            using (SqlConnection conn = new SqlConnection(Conexion.cn))
            using (SqlCommand cmd = new SqlCommand("sp_ReporteDashboard", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                conn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        // Intentamos leer por los nombres más probables que devuelve el SP.
                        // Si el SP devuelve 'CantidadClientes' lo tomamos; si devuelve 'TotalClientes' usamos ese.
                        if (ColumnExists(dr, "CantidadClientes"))
                            modelo.CantidadClientes = dr["CantidadClientes"] != DBNull.Value ? Convert.ToInt32(dr["CantidadClientes"]) : 0;
                        else if (ColumnExists(dr, "TotalClientes"))
                            modelo.CantidadClientes = dr["TotalClientes"] != DBNull.Value ? Convert.ToInt32(dr["TotalClientes"]) : 0;

                        if (ColumnExists(dr, "CantidadVentas"))
                            modelo.CantidadVentas = dr["CantidadVentas"] != DBNull.Value ? Convert.ToInt32(dr["CantidadVentas"]) : 0;
                        else if (ColumnExists(dr, "TotalVentas"))
                            modelo.CantidadVentas = dr["TotalVentas"] != DBNull.Value ? Convert.ToInt32(dr["TotalVentas"]) : 0;

                        if (ColumnExists(dr, "CantidadProductos"))
                            modelo.CantidadProductos = dr["CantidadProductos"] != DBNull.Value ? Convert.ToInt32(dr["CantidadProductos"]) : 0;
                        else if (ColumnExists(dr, "TotalProductos"))
                            modelo.CantidadProductos = dr["TotalProductos"] != DBNull.Value ? Convert.ToInt32(dr["TotalProductos"]) : 0;
                    }
                }
            }

            return modelo;
        }

        // Helper para verificar que la columna existe en el SqlDataReader
        private bool ColumnExists(SqlDataReader reader, string columnName)
        {
            try
            {
                return reader.GetOrdinal(columnName) >= 0;
            }
            catch
            {
                return false;
            }
        }

        // 🚀 Nuevo método para el reporte de ventas (acepta fechas nulas)
        public List<ReporteVentas> ReporteVentas(DateTime? fechaInicio, DateTime? fechaFin, string idTransaccion)
        {
            List<ReporteVentas> lista = new List<ReporteVentas>();

            using (SqlConnection conn = new SqlConnection(Conexion.cn))
            using (SqlCommand cmd = new SqlCommand("sp_ReporteVentas", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                // Agregar parámetros condicionalmente
                if (fechaInicio.HasValue)
                    cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio.Value);
                else
                    cmd.Parameters.AddWithValue("@FechaInicio", DBNull.Value);

                if (fechaFin.HasValue)
                    cmd.Parameters.AddWithValue("@FechaFin", fechaFin.Value);
                else
                    cmd.Parameters.AddWithValue("@FechaFin", DBNull.Value);

                cmd.Parameters.AddWithValue("@IdTransaccion", string.IsNullOrEmpty(idTransaccion) ? (object)DBNull.Value : idTransaccion);

                conn.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new ReporteVentas
                        {
                            FechaVenta = dr["FechaVenta"] == DBNull.Value ? string.Empty : Convert.ToDateTime(dr["FechaVenta"]).ToString("dd/MM/yyyy"),
                            Cliente = dr["Cliente"] == DBNull.Value ? string.Empty : dr["Cliente"].ToString(),
                            Producto = dr["Producto"] == DBNull.Value ? string.Empty : dr["Producto"].ToString(),
                            Precio = dr["Precio"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Precio"]),
                            Cantidad = dr["Cantidad"] == DBNull.Value ? 0 : Convert.ToInt32(dr["Cantidad"]),
                            Total = dr["Total"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Total"]),
                            IdTransaccion = dr["IdTransaccion"] == DBNull.Value ? string.Empty : dr["IdTransaccion"].ToString()
                        });
                    }
                }
            }

            return lista;
        }
    }
}
