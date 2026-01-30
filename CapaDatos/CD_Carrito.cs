//CD_Carrito.cs

using CapaEntidad;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace CapaDatos
{
    public class CD_Carrito
    {
        public bool ExisteCarrito(int idCliente, int idProducto)
        {
            bool resultado = false;
            using (SqlConnection conn = new SqlConnection(Conexion.cn))
            {
                SqlCommand cmd = new SqlCommand("sp_ExisteCarrito", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdCliente", idCliente);
                cmd.Parameters.AddWithValue("@IdProducto", idProducto);

                SqlParameter output = new SqlParameter("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(output);

                conn.Open();
                cmd.ExecuteNonQuery();
                resultado = output.Value != DBNull.Value && Convert.ToBoolean(output.Value);
            }
            return resultado;
        }

        public bool OperacionCarrito(int idCliente, int idProducto, int cantidad, out string mensaje)
        {
            bool resultado = false;
            mensaje = string.Empty;

            try
            {
                using (SqlConnection conn = new SqlConnection(Conexion.cn))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_OperacionCarrito", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@IdCliente", idCliente);
                        cmd.Parameters.AddWithValue("@IdProducto", idProducto);
                        cmd.Parameters.AddWithValue("@Cantidad", cantidad);

                        SqlParameter outResultado = new SqlParameter("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };
                        SqlParameter outMensaje = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output };
                        cmd.Parameters.Add(outResultado);
                        cmd.Parameters.Add(outMensaje);

                        conn.Open();
                        cmd.ExecuteNonQuery();

                        resultado = outResultado.Value != DBNull.Value && Convert.ToBoolean(outResultado.Value);
                        mensaje = outMensaje.Value == DBNull.Value ? string.Empty : outMensaje.Value.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                resultado = false;
                mensaje = ex.Message;
            }

            return resultado;
        }

        public int CantidadEnCarrito(int idCliente)
        {
            int cantidad = 0;
            using (SqlConnection conn = new SqlConnection(Conexion.cn))
            {
                SqlCommand cmd = new SqlCommand("sp_CantidadCarrito", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdCliente", idCliente);

                conn.Open();
                object result = cmd.ExecuteScalar();
                cantidad = (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);
            }
            return cantidad;
        }

        public List<ItemCarrito> Listar(int idCliente)
        {
            List<ItemCarrito> lista = new List<ItemCarrito>();
            using (SqlConnection conn = new SqlConnection(Conexion.cn))
            {
                string query = "SELECT * FROM fn_ListarCarrito(@IdCliente)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@IdCliente", idCliente);

                conn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new ItemCarrito
                        {
                            Producto = new Producto
                            {
                                IdProducto = dr["IdProducto"] == DBNull.Value ? 0 : Convert.ToInt32(dr["IdProducto"]),
                                Nombre = dr["Nombre"] == DBNull.Value ? string.Empty : dr["Nombre"].ToString(),
                                Precio = dr["Precio"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Precio"]),
                                NombreImagen = dr["NombreImagen"] == DBNull.Value ? string.Empty : dr["NombreImagen"].ToString(),
                                RutaImagen = dr["RutaImagen"] == DBNull.Value ? string.Empty : dr["RutaImagen"].ToString(),
                                oMarca = new Marca { Descripcion = dr["Marca"] == DBNull.Value ? string.Empty : dr["Marca"].ToString() }
                            },
                            Cantidad = dr["Cantidad"] == DBNull.Value ? 0 : Convert.ToInt32(dr["Cantidad"])
                        });
                    }
                }
            }
            return lista;
        }

        public bool ActualizarCantidad(int idCliente, int idProducto, int delta, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                using (SqlConnection conn = new SqlConnection(Conexion.cn))
                {
                    SqlCommand cmd = new SqlCommand("sp_ActualizarCantidadCarrito", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdCliente", idCliente);
                    cmd.Parameters.AddWithValue("@IdProducto", idProducto);
                    cmd.Parameters.AddWithValue("@Delta", delta);

                    conn.Open();
                    int affected = cmd.ExecuteNonQuery();
                    if (affected <= 0)
                    {
                        mensaje = "No se actualizó ninguna fila en la base de datos.";
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                mensaje = "Error al actualizar cantidad: " + ex.Message;
                return false;
            }
        }

        public bool Eliminar(int idCliente, int idProducto, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                using (SqlConnection conn = new SqlConnection(Conexion.cn))
                {
                    SqlCommand cmd = new SqlCommand("sp_EliminarCarrito", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdCliente", idCliente);
                    cmd.Parameters.AddWithValue("@IdProducto", idProducto);

                    conn.Open();
                    int affected = cmd.ExecuteNonQuery();
                    if (affected <= 0)
                    {
                        mensaje = "No se eliminó ninguna fila en la base de datos.";
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                mensaje = "Error al eliminar producto del carrito: " + ex.Message;
                return false;
            }
        }

        public bool Vaciar(int idCliente, out string mensaje)
        {
            bool resultado = false;
            mensaje = string.Empty;

            try
            {
                using (var conn = new SqlConnection(Conexion.cn))
                using (var cmd = new SqlCommand("sp_VaciarCarrito", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdCliente", idCliente);

                    var outResultado = new SqlParameter("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };
                    var outMensaje = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output };

                    cmd.Parameters.Add(outResultado);
                    cmd.Parameters.Add(outMensaje);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    resultado = outResultado.Value != DBNull.Value && Convert.ToBoolean(outResultado.Value);
                    mensaje = outMensaje.Value == DBNull.Value ? string.Empty : outMensaje.Value.ToString();
                }
            }
            catch (Exception ex)
            {
                resultado = false;
                mensaje = ex.Message;
            }

            return resultado;
        }

        public bool Agregar(int idCliente, int idProducto, int cantidad, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                return OperacionCarrito(idCliente, idProducto, cantidad, out mensaje);
            }
            catch (Exception ex)
            {
                mensaje = ex.Message;
                return false;
            }
        }
    }
}
