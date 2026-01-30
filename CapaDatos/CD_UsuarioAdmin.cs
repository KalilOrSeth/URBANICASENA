using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using CapaEntidad;

namespace CapaDatos
{
    public class CD_UsuarioAdmin
    {
        // List all admins
        public List<Usuario> Listar()
        {
            var lista = new List<Usuario>();
            try
            {
                using (SqlConnection oconexion = new SqlConnection(Conexion.cn))
                using (SqlCommand cmd = new SqlCommand("SELECT IdUsuario,Nombres,Apellidos,Correo,Clave,Reestablecer,Activo,Rol FROM UsuarioAdmin", oconexion))
                {
                    cmd.CommandType = CommandType.Text;
                    oconexion.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var u = new Usuario
                            {
                                IdUsuario = Convert.ToInt32(dr["IdUsuario"]),
                                Nombres = dr["Nombres"].ToString(),
                                Apellidos = dr["Apellidos"].ToString(),
                                Correo = dr["Correo"].ToString(),
                                Clave = dr["Clave"].ToString(),
                                Reestablecer = dr["Reestablecer"] != DBNull.Value && Convert.ToBoolean(dr["Reestablecer"]),
                                Activo = dr["Activo"] != DBNull.Value && Convert.ToBoolean(dr["Activo"]),
                                Rol = dr["Rol"] != DBNull.Value ? dr["Rol"].ToString() : null
                            };
                            lista.Add(u);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("CD_UsuarioAdmin.Listar: " + ex.Message);
            }
            return lista;
        }

        // Obtener admin por correo (case-insensitive trim)
        public Usuario ObtenerPorCorreo(string correo)
        {
            try
            {
                using (SqlConnection oconexion = new SqlConnection(Conexion.cn))
                using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 IdUsuario,Nombres,Apellidos,Correo,Clave,Reestablecer,Activo,Rol FROM UsuarioAdmin WHERE LOWER(LTRIM(RTRIM(Correo))) = @Correo", oconexion))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@Correo", correo?.Trim().ToLower() ?? string.Empty);
                    oconexion.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            return new Usuario
                            {
                                IdUsuario = Convert.ToInt32(dr["IdUsuario"]),
                                Nombres = dr["Nombres"].ToString(),
                                Apellidos = dr["Apellidos"].ToString(),
                                Correo = dr["Correo"].ToString(),
                                Clave = dr["Clave"].ToString(),
                                Reestablecer = dr["Reestablecer"] != DBNull.Value && Convert.ToBoolean(dr["Reestablecer"]),
                                Activo = dr["Activo"] != DBNull.Value && Convert.ToBoolean(dr["Activo"]),
                                Rol = dr["Rol"] != DBNull.Value ? dr["Rol"].ToString() : null
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("CD_UsuarioAdmin.ObtenerPorCorreo: " + ex.Message);
            }
            return null;
        }

        // Registrar admin: prefer stored procedure SP_RegistrarUsuarioAdmin, fallback to direct INSERT into UsuarioAdmin
        public int Registrar(Usuario obj, out string Mensaje)
        {
            Mensaje = string.Empty;
            int idautogenerado = 0;
            try
            {
                using (SqlConnection oconexion = new SqlConnection(Conexion.cn))
                using (SqlCommand cmd = new SqlCommand("SP_RegistrarUsuarioAdmin", oconexion))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("Nombres", obj.Nombres ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("Apellidos", obj.Apellidos ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("Correo", obj.Correo ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("Clave", obj.Clave ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("Activo", obj.Activo);
                    cmd.Parameters.AddWithValue("Rol", "Admin");

                    cmd.Parameters.Add("Resultado", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;

                    oconexion.Open();
                    cmd.ExecuteNonQuery();

                    idautogenerado = Convert.ToInt32(cmd.Parameters["Resultado"].Value);
                    Mensaje = cmd.Parameters["Mensaje"].Value.ToString();
                    return idautogenerado;
                }
            }
            catch (SqlException sqlEx)
            {
                Debug.WriteLine("CD_UsuarioAdmin.Registrar SP failed: " + sqlEx.Message);
                try
                {
                    using (SqlConnection oconexion = new SqlConnection(Conexion.cn))
                    {
                        string insert = "INSERT INTO UsuarioAdmin (Nombres,Apellidos,Correo,Clave,Activo,Rol) VALUES (@Nombres,@Apellidos,@Correo,@Clave,@Activo,@Rol); SELECT CAST(SCOPE_IDENTITY() AS INT);";
                        using (SqlCommand cmd = new SqlCommand(insert, oconexion))
                        {
                            cmd.CommandType = CommandType.Text;
                            cmd.Parameters.AddWithValue("@Nombres", obj.Nombres ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Apellidos", obj.Apellidos ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Correo", obj.Correo ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Clave", obj.Clave ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Activo", obj.Activo);
                            cmd.Parameters.AddWithValue("@Rol", obj.Rol ?? "Admin");

                            oconexion.Open();
                            var result = cmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                idautogenerado = Convert.ToInt32(result);
                                Mensaje = "Registro insertado en UsuarioAdmin (fallback).";
                                return idautogenerado;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Mensaje = "CD_UsuarioAdmin.Registrar fallback failed: " + ex.Message;
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Mensaje = ex.Message;
            }
            return idautogenerado;
        }

        // Actualizar clave en UsuarioAdmin
        public bool ActualizarClave(int idUsuario, string claveHash, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                using (SqlConnection oconexion = new SqlConnection(Conexion.cn))
                using (SqlCommand cmd = new SqlCommand("UPDATE UsuarioAdmin SET Clave = @Clave WHERE IdUsuario = @IdUsuario", oconexion))
                {
                    cmd.Parameters.AddWithValue("@Clave", claveHash ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    cmd.CommandType = CommandType.Text;
                    oconexion.Open();
                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0;
                }
            }
            catch (Exception ex)
            {
                mensaje = ex.Message;
                return false;
            }
        }

        // Editar admin
        public bool Editar(Usuario obj, out string Mensaje)
        {
            Mensaje = string.Empty;
            try
            {
                using (SqlConnection oconexion = new SqlConnection(Conexion.cn))
                {
                    string update = "UPDATE UsuarioAdmin SET Nombres=@Nombres,Apellidos=@Apellidos,Correo=@Correo,Activo=@Activo,Rol=@Rol WHERE IdUsuario=@IdUsuario";
                    using (var cmd = new SqlCommand(update, oconexion))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@Nombres", obj.Nombres ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Apellidos", obj.Apellidos ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Correo", obj.Correo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Activo", obj.Activo);
                        cmd.Parameters.AddWithValue("@Rol", obj.Rol ?? "Admin");
                        cmd.Parameters.AddWithValue("@IdUsuario", obj.IdUsuario);

                        oconexion.Open();
                        int rows = cmd.ExecuteNonQuery();
                        if (rows > 0) return true;
                        Mensaje = "No se actualizó ningún registro.";
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Mensaje = ex.Message;
                return false;
            }
        }

        // Eliminar admin
        public bool Eliminar(int idusuario, out string Mensaje)
        {
            Mensaje = string.Empty;
            try
            {
                using (SqlConnection oconexion = new SqlConnection(Conexion.cn))
                using (var cmd = new SqlCommand("DELETE FROM UsuarioAdmin WHERE IdUsuario=@IdUsuario", oconexion))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@IdUsuario", idusuario);
                    oconexion.Open();
                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0) return true;
                    Mensaje = "No se eliminó ningún registro.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                Mensaje = ex.Message;
                return false;
            }
        }

        public bool CambiarClavePorCorreo(string correo, string nuevaClave, out string mensaje)
        {
            mensaje = string.Empty;
            bool resultado = false;

            try
            {
                using (SqlConnection conn = new SqlConnection(Conexion.cn))
                {
                    conn.Open();

                    // 0. Validar si el correo existe en UsuarioAdmin
                    string checkCorreo = "SELECT COUNT(*) FROM UsuarioAdmin WHERE Correo = @Correo";
                    SqlCommand cmdCheck = new SqlCommand(checkCorreo, conn);
                    cmdCheck.Parameters.AddWithValue("@Correo", correo);
                    int existe = Convert.ToInt32(cmdCheck.ExecuteScalar());

                    if (existe == 0)
                    {
                        mensaje = "El correo no está registrado como administrador.";
                        return false; // detenemos el flujo
                    }

                    // 1. Actualizar la clave en UsuarioAdmin
                    string updateClave = "UPDATE UsuarioAdmin SET Clave = @Clave, Reestablecer = NULL WHERE Correo = @Correo";
                    SqlCommand cmdUpdate = new SqlCommand(updateClave, conn);
                    cmdUpdate.Parameters.AddWithValue("@Clave", nuevaClave);
                    cmdUpdate.Parameters.AddWithValue("@Correo", correo);

                    int filas = cmdUpdate.ExecuteNonQuery();
                    resultado = filas > 0;

                    if (!resultado)
                        mensaje = "No se pudo actualizar la contraseña.";
                }
            }
            catch (Exception ex)
            {
                mensaje = ex.Message;
                resultado = false;
            }

            return resultado;
        }

        // Update admin email by IdUsuario, ensuring no duplicate in Cliente/UsuarioNormal
        public bool ActualizarCorreo(int idUsuario, string nuevoCorreo, out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                using (SqlConnection conn = new SqlConnection(Conexion.cn))
                {
                    conn.Open();
                    // Normalize
                    string correoNorm = (nuevoCorreo ?? string.Empty).Trim().ToLower();

                    // Check Cliente and UsuarioNormal for existing correo
                    string checkCliente = "SELECT COUNT(*) FROM Cliente WHERE LOWER(LTRIM(RTRIM(Correo))) = @Correo";
                    using (SqlCommand cmdChk = new SqlCommand(checkCliente, conn))
                    {
                        cmdChk.Parameters.AddWithValue("@Correo", correoNorm);
                        int existeCliente = Convert.ToInt32(cmdChk.ExecuteScalar());
                        if (existeCliente > 0)
                        {
                            mensaje = "El correo ya está registrado como cliente.";
                            return false;
                        }
                    }

                    string checkUsuarioNormal = "SELECT COUNT(*) FROM UsuarioNormal WHERE LOWER(LTRIM(RTRIM(Correo))) = @Correo";
                    using (SqlCommand cmdChk2 = new SqlCommand(checkUsuarioNormal, conn))
                    {
                        cmdChk2.Parameters.AddWithValue("@Correo", correoNorm);
                        int existeUser = Convert.ToInt32(cmdChk2.ExecuteScalar());
                        if (existeUser > 0)
                        {
                            mensaje = "El correo ya está registrado en otra cuenta.";
                            return false;
                        }
                    }

                    // Update UsuarioAdmin
                    string update = "UPDATE UsuarioAdmin SET Correo = @Correo WHERE IdUsuario = @IdUsuario";
                    using (SqlCommand cmdUpd = new SqlCommand(update, conn))
                    {
                        cmdUpd.Parameters.AddWithValue("@Correo", nuevoCorreo ?? (object)DBNull.Value);
                        cmdUpd.Parameters.AddWithValue("@IdUsuario", idUsuario);

                        int filas = cmdUpd.ExecuteNonQuery();
                        if (filas <= 0)
                        {
                            mensaje = "No se encontró el administrador a actualizar.";
                            return false;
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                mensaje = ex.Message;
                return false;
            }
        }

    }
}
