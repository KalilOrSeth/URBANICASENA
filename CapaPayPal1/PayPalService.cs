// CapaPayPal/PayPalService.cs
using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;
using Microsoft.Extensions.Configuration;

public class PayPalService
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _mode;
    private readonly HttpClient _httpClient;

    public PayPalService(IConfiguration configuration)
    {
        _clientId = configuration["PayPal:ClientId"];
        _clientSecret = configuration["PayPal:ClientSecret"];
        _mode = configuration["PayPal:Mode"];
        _httpClient = new HttpClient();
    }

    public async Task<PayPalCheckoutSdk.Orders.Order> CreateOrderAsync(decimal amount, string returnUrl, string cancelUrl)
    {
        var accessToken = await GetAccessTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var request = new OrdersCreateRequest();
        request.Prefer("return=representation");
        request.RequestBody(new OrderRequest
        {
            CheckoutPaymentIntent = "CAPTURE",
            PurchaseUnits = new List<PurchaseUnitRequest>
            {
                new PurchaseUnitRequest
                {
                    AmountWithBreakdown = new AmountWithBreakdown
                    {
                        CurrencyCode = "USD",
                        Value = amount.ToString()
                    }
                }
            },
            ApplicationContext = new ApplicationContext
            {
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl
            }
        });

        var response = await _httpClient.Execute(request);
        return response.Result<Order>();
    }

    public async Task<PayPalCheckoutSdk.Orders.Order> CaptureOrderAsync(string orderId)
    {
        var accessToken = await GetAccessTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var request = new OrdersCaptureRequest(orderId);
        request.RequestBody(new OrderActionRequest());

        var response = await _httpClient.Execute(request);
        return response.Result<Order>();
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var auth = new ClientCredentialsTokenRequest
        {
            ClientId = _clientId,
            ClientSecret = _clientSecret,
            Scope = "https://api.paypal.com/v1/payments/*"
        };

        var client = new PayPalHttpClient(new PayPalEnvironment(_mode, _clientId, _clientSecret));
        var token = await client.ExecuteAsync(auth);
        return token.Result.AccessToken;
    }
}