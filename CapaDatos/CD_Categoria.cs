using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using CapaEntidad;

namespace CapaDatos
{
    public class CD_Categoria
    {
        public List<Categoria> Listar()
        {
            var lista = new List<Categoria>();

            using (var conexion = new SqlConnection(Conexion.cn))
            {
                try
                {
                    using (var cmd = new SqlCommand("SP_ListarCategorias", conexion))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        conexion.Open();

                        using (var dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                lista.Add(new Categoria
                                {
                                    IdCategoria = Convert.ToInt32(dr["IdCategoria"]),
                                    Descripcion = dr["Descripcion"].ToString(),
                                    Activo = Convert.ToBoolean(dr["Activo"])
                                });
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    lista = new List<Categoria>();
                }
            }

            return lista;
        }

        public int Registrar(Categoria obj, out string Mensaje)
        {
            int idGenerado = 0;
            Mensaje = string.Empty;

            using (var conexion = new SqlConnection(Conexion.cn))
            {
                try
                {
                    using (var cmd = new SqlCommand("SP_RegistrarCategoria", conexion))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Descripcion", obj.Descripcion);
                        cmd.Parameters.AddWithValue("@Activo", obj.Activo);
                        cmd.Parameters.Add("@Resultado", SqlDbType.Int).Direction = ParameterDirection.Output;

                        conexion.Open();
                        cmd.ExecuteNonQuery();

                        idGenerado = Convert.ToInt32(cmd.Parameters["@Resultado"].Value);
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

        public bool Editar(Categoria obj, out string Mensaje)
        {
            bool resultado = false;
            Mensaje = string.Empty;

            using (var conexion = new SqlConnection(Conexion.cn))
            {
                try
                {
                    using (var cmd = new SqlCommand("SP_EditarCategoria", conexion))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@IdCategoria", obj.IdCategoria);
                        cmd.Parameters.AddWithValue("@Descripcion", obj.Descripcion);
                        cmd.Parameters.AddWithValue("@Activo", obj.Activo);
                        cmd.Parameters.Add("@Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;

                        conexion.Open();
                        cmd.ExecuteNonQuery();

                        resultado = Convert.ToBoolean(cmd.Parameters["@Resultado"].Value);
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

        public bool Eliminar(int idCategoria, out string Mensaje)
        {
            bool resultado = false;
            Mensaje = string.Empty;

            using (var conexion = new SqlConnection(Conexion.cn))
            {
                try
                {
                    using (var cmd = new SqlCommand("SP_EliminarCategoria", conexion))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@IdCategoria", idCategoria);
                        cmd.Parameters.Add("@Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;

                        conexion.Open();
                        cmd.ExecuteNonQuery();

                        resultado = Convert.ToBoolean(cmd.Parameters["@Resultado"].Value);
                    }
                }
                catch (Exception ex)
                {
                    resultado = false;
                    // Detect SQL Server foreign key violation (error number 547) and return friendlier message
                    if (ex is SqlException sqlEx && sqlEx.Number == 547)
                    {
                        Mensaje = "No se puede eliminar la categoría porque está asociada a productos.";
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
