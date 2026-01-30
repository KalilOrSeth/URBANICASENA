using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using CapaEntidad;

namespace CapaDatos
{
    public class CD_Productos
    {
        public List<Producto> Listar()
        {
            var lista = new List<Producto>();

            using (var conexion = new SqlConnection(Conexion.cn))
            {
                try
                {
                    string query = @"
                SELECT p.IdProducto, p.Nombre, p.Descripcion,
                       m.IdMarca, m.Descripcion AS DesMarca,
                       c.IdCategoria, c.Descripcion AS DesCategoria,
                       p.Precio, p.Stock, p.RutaImagen, p.NombreImagen,
                       p.Activo
                FROM Producto p
                INNER JOIN Marca m ON m.IdMarca = p.IdMarca
                INNER JOIN Categoria c ON c.IdCategoria = p.IdCategoria";

                    using (var cmd = new SqlCommand(query, conexion))
                    {
                        cmd.CommandType = CommandType.Text;
                        conexion.Open();

                        using (var dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                var ruta = (dr["RutaImagen"] == DBNull.Value ? "" : dr["RutaImagen"].ToString());
                                var nombre = (dr["NombreImagen"] == DBNull.Value ? "" : dr["NombreImagen"].ToString());

                                // Garantizar una ruta válida para la vista
                                if (string.IsNullOrWhiteSpace(ruta) && string.IsNullOrWhiteSpace(nombre))
                                {
                                    ruta = "~/Content/img/productos/";
                                    nombre = "default.jpg";
                                }
                                else
                                {
                                    // Si falta la parte de ruta, aseguramos que haya una carpeta base
                                    if (string.IsNullOrWhiteSpace(ruta))
                                    {
                                        ruta = "~/Content/img/productos/";
                                    }
                                    // Si falta el nombre, usamos el default
                                    if (string.IsNullOrWhiteSpace(nombre))
                                    {
                                        nombre = "default.jpg";
                                    }
                                }

                                lista.Add(new Producto
                                {
                                    IdProducto = Convert.ToInt32(dr["IdProducto"]),
                                    Nombre = dr["Nombre"].ToString(),
                                    Descripcion = dr["Descripcion"].ToString(),
                                    oMarca = new Marca
                                    {
                                        IdMarca = Convert.ToInt32(dr["IdMarca"]),
                                        Descripcion = dr["DesMarca"].ToString()
                                    },
                                    oCategoria = new Categoria
                                    {
                                        IdCategoria = Convert.ToInt32(dr["IdCategoria"]),
                                        Descripcion = dr["DesCategoria"].ToString()
                                    },
                                    Precio = Convert.ToDecimal(dr["Precio"]),
                                    Stock = Convert.ToInt32(dr["Stock"]),
                                    // Asignar las partes normalizadas
                                    RutaImagen = ruta,
                                    NombreImagen = nombre,
                                    Activo = Convert.ToBoolean(dr["Activo"])
                                });
                            }
                        }
                    }
                }
                catch
                {
                    lista = new List<Producto>();
                }
            }

            return lista;
        }

        // Agregado: métodos esperados por CN_Productos
        public int Registrar(Producto obj, out string Mensaje)
        {
            Mensaje = string.Empty;
            int idGenerado = 0;

            try
            {
                using (var conexion = new SqlConnection(Conexion.cn))
                {
                    using (var cmd = new SqlCommand("SP_RegistrarProducto", conexion))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Nombre", obj.Nombre ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Descripcion", obj.Descripcion ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@IdMarca", obj.oMarca?.IdMarca ?? 0);
                        cmd.Parameters.AddWithValue("@IdCategoria", obj.oCategoria?.IdCategoria ?? 0);
                        cmd.Parameters.AddWithValue("@Precio", obj.Precio);
                        cmd.Parameters.AddWithValue("@Stock", obj.Stock);
                        cmd.Parameters.AddWithValue("@Activo", obj.Activo);

                        SqlParameter paramId = new SqlParameter("@IdGenerado", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(paramId);

                        SqlParameter paramMsg = new SqlParameter("@Mensaje", SqlDbType.VarChar, 200)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(paramMsg);

                        conexion.Open();
                        cmd.ExecuteNonQuery();

                        if (paramId.Value != DBNull.Value)
                            idGenerado = Convert.ToInt32(paramId.Value);

                        Mensaje = paramMsg.Value.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Mensaje = ex.Message;
                idGenerado = 0;
            }

            return idGenerado;
        }


        public bool Editar(Producto obj, out string Mensaje)
        {
            Mensaje = string.Empty;
            try
            {
                using (var conexion = new SqlConnection(Conexion.cn))
                using (var cmd = new SqlCommand("SP_EditarProducto", conexion))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdProducto", obj.IdProducto);
                    cmd.Parameters.AddWithValue("@Nombre", obj.Nombre ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Descripcion", obj.Descripcion ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@IdMarca", obj.oMarca?.IdMarca ?? 0);
                    cmd.Parameters.AddWithValue("@IdCategoria", obj.oCategoria?.IdCategoria ?? 0);
                    cmd.Parameters.AddWithValue("@Precio", obj.Precio);
                    cmd.Parameters.AddWithValue("@Stock", obj.Stock);
                    cmd.Parameters.AddWithValue("@Activo", obj.Activo);

                    // output parameters expected by SP
                    var paramResultado = new SqlParameter("@Resultado", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(paramResultado);
                    var paramMensaje = new SqlParameter("@Mensaje", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(paramMensaje);

                    conexion.Open();
                    cmd.ExecuteNonQuery();

                    if (paramMensaje.Value != DBNull.Value)
                        Mensaje = paramMensaje.Value.ToString();

                    int resultado = 0;
                    if (paramResultado.Value != DBNull.Value)
                        resultado = Convert.ToInt32(paramResultado.Value);

                    return resultado == 1;
                }
            }
            catch (Exception ex)
            {
                Mensaje = ex.Message;
                return false;
            }
        }

        public bool Eliminar(int idProducto, out string Mensaje)
        {
            Mensaje = string.Empty;
            try
            {
                using (var conexion = new SqlConnection(Conexion.cn))
                using (var cmd = new SqlCommand("SP_EliminarProducto", conexion))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdProducto", idProducto);

                    conexion.Open();
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Detect FK violation and return friendly message
                if (ex is SqlException sqlEx && sqlEx.Number == 547)
                {
                    Mensaje = "No se puede eliminar el producto porque está referenciado en otras tablas.";
                }
                else
                {
                    Mensaje = ex.Message;
                }
                return false;
            }
        }

        public bool GuardarDatosImagen(int idProducto, string rutaImagen, string nombreImagen, out string mensaje)
        {
            mensaje = string.Empty;
            bool resultado = false;

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.cn))
                {
                    using (SqlCommand cmd = new SqlCommand("SP_GuardarDatosImagenProducto", cn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@IdProducto", idProducto);
                        cmd.Parameters.AddWithValue("@RutaImagen", rutaImagen ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@NombreImagen", nombreImagen ?? (object)DBNull.Value);

                        SqlParameter outputResultado = new SqlParameter("@Resultado", SqlDbType.Bit)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(outputResultado);

                        SqlParameter outputMensaje = new SqlParameter("@Mensaje", SqlDbType.VarChar, 500)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(outputMensaje);

                        cn.Open();
                        cmd.ExecuteNonQuery();

                        resultado = Convert.ToBoolean(outputResultado.Value);
                        mensaje = outputMensaje.Value.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                mensaje = ex.Message;
                resultado = false;
            }

            return resultado;
        }


        // Nuevo: ObtenerPorId
        public Producto ObtenerPorId(int id)
        {
            Producto producto = null;

            using (var conexion = new SqlConnection(Conexion.cn))
            {
                try
                {
                    string query = @"
                SELECT p.IdProducto, p.Nombre, p.Descripcion,
                       m.IdMarca, m.Descripcion AS DesMarca,
                       c.IdCategoria, c.Descripcion AS DesCategoria,
                       p.Precio, p.Stock, p.RutaImagen, p.NombreImagen,
                       p.Activo
                FROM Producto p
                INNER JOIN Marca m ON m.IdMarca = p.IdMarca
                INNER JOIN Categoria c ON c.IdCategoria = p.IdCategoria
                WHERE p.IdProducto = @IdProducto";

                    using (var cmd = new SqlCommand(query, conexion))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@IdProducto", id);
                        conexion.Open();

                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                var ruta = (dr["RutaImagen"] == DBNull.Value ? "" : dr["RutaImagen"].ToString());
                                var nombre = (dr["NombreImagen"] == DBNull.Value ? "" : dr["NombreImagen"].ToString());

                                if (string.IsNullOrWhiteSpace(ruta) && string.IsNullOrWhiteSpace(nombre))
                                {
                                    ruta = "~/Content/img/productos/";
                                    nombre = "default.jpg";
                                }
                                else
                                {
                                    if (string.IsNullOrWhiteSpace(ruta)) ruta = "~/Content/img/productos/";
                                    if (string.IsNullOrWhiteSpace(nombre)) nombre = "default.jpg";
                                }

                                producto = new Producto
                                {
                                    IdProducto = Convert.ToInt32(dr["IdProducto"]),
                                    Nombre = dr["Nombre"].ToString(),
                                    Descripcion = dr["Descripcion"].ToString(),
                                    oMarca = new Marca
                                    {
                                        IdMarca = Convert.ToInt32(dr["IdMarca"]),
                                        Descripcion = dr["DesMarca"].ToString()
                                    },
                                    oCategoria = new Categoria
                                    {
                                        IdCategoria = Convert.ToInt32(dr["IdCategoria"]),
                                        Descripcion = dr["DesCategoria"].ToString()
                                    },
                                    Precio = Convert.ToDecimal(dr["Precio"]),
                                    Stock = Convert.ToInt32(dr["Stock"]),
                                    RutaImagen = ruta,
                                    NombreImagen = nombre,
                                    Activo = Convert.ToBoolean(dr["Activo"])
                                };
                            }
                        }
                    }
                }
                catch
                {
                    producto = null;
                }
            }

            return producto;
        }
    }
}
