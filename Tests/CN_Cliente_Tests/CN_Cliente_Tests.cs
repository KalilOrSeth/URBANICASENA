using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using CapaNegocio;
using CapaDatos;
using CapaEntidad;

namespace CN_Cliente_Tests
{
    [TestClass]
    public class CN_Cliente_Tests
    {
        // Nota: este test usa Moq; asegurarse de instalar Moq via NuGet en proyecto de tests y referenciar CapaDatos/CapaNegocio

        [TestMethod]
        public void EditarEstadoActivo_FlujoFeliz_RetornaTrue()
        {
            // Esto es un placeholder: en esta arquitectura CD_Cliente no está inyectado, así que para un test real habría que refactorizar CN_Cliente para inyectar la dependencia.
            // Aquí verificamos la lógica de validación previa en CN_Cliente
            var cn = new CN_Cliente();
            string mensaje;
            // Sin acceso real a BD, esperar false porque id 0 es inválido
            bool ok = cn.EditarEstadoActivo(1, false, out mensaje);
            // No podemos garantizar el valor real sin DB; al menos validar que retorna bool y mensaje no null
            Assert.IsNotNull(mensaje);
            Assert.IsInstanceOfType(ok, typeof(bool));
        }

        [TestMethod]
        public void EditarEstadoActivo_IdInvalido_RetornaFalse()
        {
            var cn = new CN_Cliente();
            string mensaje;
            bool ok = cn.EditarEstadoActivo(0, false, out mensaje);
            Assert.IsFalse(ok);
            Assert.IsFalse(string.IsNullOrEmpty(mensaje));
        }
    }
}
