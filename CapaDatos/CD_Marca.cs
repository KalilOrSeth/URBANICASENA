using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using CapaEntidad;

namespace CapaDatos
{
    public class CD_Marca
    {
        public List<Marca> Listar()
        {
            var lista = new List<Marca>();

            using (var conexion = new SqlConnection(Conexion.cn))
            {
                try
                {
                    using (var cmd = new SqlCommand("SP_ListarMarcas", conexion))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        conexion.Open();

                        using (var dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                lista.Add(new Marca
                                {
                                    IdMarca = Convert.ToInt32(dr["IdMarca"]),
                                    Descripcion = dr["Descripcion"].ToString(),
                                    Activo = Convert.ToBoolean(dr["Activo"])
                                });
                            }
                        }
                    }
                }
                catch
                {
                    lista = new List<Marca>();
                }
            }

            return lista;
        }

        public int Registrar(Marca obj, out string Mensaje)
        {
            int idGenerado = 0;
            Mensaje = string.Empty;

            using (var conexion = new SqlConnection(Conexion.cn))
            {
                try
                {
                    using (var cmd = new SqlCommand("SP_RegistrarMarca", conexion))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Descripcion", obj.Descripcion);
                        cmd.Parameters.AddWithValue("@Activo", obj.Activo);

                        // Parámetros de salida del SP
                        cmd.Parameters.Add("@IdGenerado", SqlDbType.Int).Direction = ParameterDirection.Output;
                        cmd.Parameters.Add("@Mensaje", SqlDbType.VarChar, 200).Direction = ParameterDirection.Output;

                        conexion.Open();
                        cmd.ExecuteNonQuery();

                        idGenerado = Convert.ToInt32(cmd.Parameters["@IdGenerado"].Value);
                        Mensaje = cmd.Parameters["@Mensaje"].Value.ToString();
                    }
                }
                catch (Exception ex)
                {
                    idGenerado = 0;
                    Mensaje = ex.Message;
                }
            }

            return idGenerado;
        }

        public bool Editar(Marca obj, out string Mensaje)
        {
            bool resultado = false;
            Mensaje = string.Empty;

            using (var conexion = new SqlConnection(Conexion.cn))
            {
                try
                {
                    using (var cmd = new SqlCommand("SP_EditarMarca", conexion))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@IdMarca", obj.IdMarca);
                        cmd.Parameters.AddWithValue("@Descripcion", obj.Descripcion);
                        cmd.Parameters.AddWithValue("@Activo", obj.Activo);

                        // Parámetros de salida del SP
                        cmd.Parameters.Add("@Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                        cmd.Parameters.Add("@Mensaje", SqlDbType.VarChar, 200).Direction = ParameterDirection.Output;

                        conexion.Open();
                        cmd.ExecuteNonQuery();

                        resultado = Convert.ToBoolean(cmd.Parameters["@Resultado"].Value);
                        Mensaje = cmd.Parameters["@Mensaje"].Value.ToString();
                    }
                }
                catch (Exception ex)
                {
                    resultado = false;
                    Mensaje = ex.Message;
                }
            }

            return resultado;
        }

        public bool Eliminar(int idMarca, out string Mensaje)
        {
            bool resultado = false;
            Mensaje = string.Empty;

            using (var conexion = new SqlConnection(Conexion.cn))
            {
                try
                {
                    using (var cmd = new SqlCommand("SP_EliminarMarca", conexion))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@IdMarca", idMarca);

                        // Parámetro de salida del SP
                        cmd.Parameters.Add("@Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;

                        conexion.Open();
                        cmd.ExecuteNonQuery();

                        resultado = Convert.ToBoolean(cmd.Parameters["@Resultado"].Value);
                    }
                }
                catch (Exception ex)
                {
                    resultado = false;
                    // Friendly message on FK violation
                    if (ex is SqlException sqlEx && sqlEx.Number == 547)
                    {
                        Mensaje = "No se puede eliminar la marca porque está asociada a productos.";
                    }
                    else
                    {
                        Mensaje = ex.Message;
                    }
                }
            }

            return resultado;
        }
    }
}
