namespace CapaEntidad
{
    public class UsuarioNormal
    {
        public int IdCliente { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Correo { get; set; }
        public string Clave { get; set; }
        public bool Reestablecer { get; set; }
    }
}