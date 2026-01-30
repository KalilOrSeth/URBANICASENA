using Dapper;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

public class CDPagos
{
    private readonly string _connectionString;

    public CDPagos(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public async Task<int> GuardarPagoAsync(int ventaId, string paypalOrderId, decimal monto)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            var query = @"
                INSERT INTO Pagos (VentaId, PayPalOrderId, Monto, FechaCreacion, Estado)
                VALUES (@VentaId, @PayPalOrderId, @Monto, GETDATE(), 'CREATED');
                SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<int>(query, new
            {
                VentaId = ventaId,
                PayPalOrderId = paypalOrderId,
                Monto = monto
            });
        }
    }

    public async Task ActualizarEstadoPagoAsync(int pagoId, string estado)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            var query = "UPDATE Pagos SET Estado = @Estado WHERE PagoId = @PagoId";
            await connection.ExecuteAsync(query, new { Estado = estado, PagoId = pagoId });
        }
    }

    public async Task<int> ObtenerPagoIdPorOrderIdAsync(string orderId)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            var query = "SELECT PagoId FROM Pagos WHERE PayPalOrderId = @OrderId";
            return await connection.QuerySingleOrDefaultAsync<int>(query, new { OrderId = orderId });
        }
    }
}