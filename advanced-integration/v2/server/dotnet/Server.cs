using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaypalServerSDK.Standard;
using PaypalServerSDK.Standard.Authentication;
using PaypalServerSDK.Standard.Controllers;
using PaypalServerSDK.Standard.Http.Response;
using PaypalServerSDK.Standard.Models;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace PayPalAdvancedIntegration;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(
                (context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                }
            )
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseUrls("http://localhost:8080");
                webBuilder.UseStartup<Startup>();
            });
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc().AddNewtonsoftJson();
        services.AddHttpClient();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        app.UseRouting();
        app.UseStaticFiles();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}

[ApiController]
public class CheckoutController : Controller
{
    private readonly OrdersController _ordersController;
    private readonly PaymentsController _paymentsController;

    private IConfiguration _configuration { get; }
    private string _paypalClientId
    {
        get { return _configuration["PAYPAL_CLIENT_ID"]; }
    }
    private string _paypalClientSecret
    {
        get { return _configuration["PAYPAL_CLIENT_SECRET"]; }
    }

    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(IConfiguration configuration, ILogger<CheckoutController> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Initialize the PayPal SDK client
        PaypalServerSDKClient client = new PaypalServerSDKClient.Builder()
            .Environment(PaypalServerSDK.Standard.Environment.Sandbox)
            .ClientCredentialsAuth(
                new ClientCredentialsAuthModel.Builder(_paypalClientId, _paypalClientSecret).Build()
            )
            .LoggingConfig(config =>
                config
                    .LogLevel(LogLevel.Information)
                    .RequestConfig(reqConfig => reqConfig.Body(true))
                    .ResponseConfig(respConfig => respConfig.Headers(true))
            )
            .Build();

        _ordersController = client.OrdersController;
        _paymentsController = client.PaymentsController;
    }

    [HttpPost("api/orders")]
    public async Task<IActionResult> CreateOrder([FromBody] dynamic cart)
    {
        try
        {
            var result = await _CreateOrder(cart);
            return StatusCode((int)result.StatusCode, result.Data);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Failed to create order:", ex);
            return StatusCode(500, new { error = "Failed to create order." });
        }
    }

    [HttpPost("api/orders/{orderID}/capture")]
    public async Task<IActionResult> CaptureOrder(string orderID)
    {
        try
        {
            var result = await _CaptureOrder(orderID);
            return StatusCode((int)result.StatusCode, result.Data);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Failed to capture order:", ex);
            return StatusCode(500, new { error = "Failed to capture order." });
        }
    }

    [HttpPost("api/orders/{orderID}/authorize")]
    public async Task<IActionResult> AuthorizeOrder(string orderID)
    {
        try
        {
            var result = await _AuthorizeOrder(orderID);
            return StatusCode((int)result.StatusCode, result.Data);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Failed to authorize order:", ex);
            return StatusCode(500, new { error = "Failed to authorize order." });
        }
    }

    [HttpPost("api/orders/{authorizationID}/captureAuthorize")]
    public async Task<IActionResult> CaptureAuthorizeOrder(string authorizationID)
    {
        try
        {
            var result = await _CaptureAuthorizeOrder(authorizationID);
            return StatusCode((int)result.StatusCode, result.Data);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Failed to authorize order:", ex);
            return StatusCode(500, new { error = "Failed to authorize order." });
        }
    }

    [HttpPost("api/payments/refund")]
    public async Task<IActionResult> RefundCapture([FromBody] dynamic body)
    {
        try
        {
            var result = await _RefundCapture((string) body.capturedPaymentId);
            return StatusCode((int)result.StatusCode, result.Data);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Failed to refund capture:", ex);
            return StatusCode(500, new { error = "Failed to refund capture." });
        }
    }

    private async Task<dynamic> _CreateOrder(dynamic cart)
    {
        OrdersCreateInput ordersCreateInput = new OrdersCreateInput
        {
            Body = new OrderRequest
            {
                Intent = CheckoutPaymentIntent.CAPTURE,
                PurchaseUnits = new List<PurchaseUnitRequest>
                {
                    new PurchaseUnitRequest
                    {
                        Amount = new AmountWithBreakdown { CurrencyCode = "USD", MValue = "100", },
                        Shipping = new ShippingDetails
                        {
                            Options = new List<ShippingOption>
                            {
                                new ShippingOption
                                {
                                    Id = "1",
                                    Label = "Free Shipping",
                                    Selected = true,
                                    Type = ShippingType.SHIPPING,
                                    Amount = new Money { CurrencyCode = "USD", MValue = "0", },
                                },
                                new ShippingOption
                                {
                                    Id = "2",
                                    Label = "USPS Priority Shipping",
                                    Selected = false,
                                    Type = ShippingType.SHIPPING,
                                    Amount = new Money { CurrencyCode = "USD", MValue = "5", },
                                },
                            },
                        },
                    },
                },

                PaymentSource = new PaymentSource
                {
                    Card = new CardRequest
                    {
                        Attributes = new CardAttributes
                        {
                            Verification = new CardVerification
                            {
                                Method = CardVerificationMethod.SCAWHENREQUIRED
                            },
                        },
                    },
                },
            },
        };

        ApiResponse<Order> result = await _ordersController.OrdersCreateAsync(ordersCreateInput);
        return result;
    }

    private async Task<dynamic> _CaptureOrder(string orderID)
    {
        OrdersCaptureInput ordersCaptureInput = new OrdersCaptureInput { Id = orderID, };

        ApiResponse<Order> result = await _ordersController.OrdersCaptureAsync(ordersCaptureInput);

        return result;
    }

    private async Task<dynamic> _CaptureAuthorizeOrder(string authorizationID)
    {
        AuthorizationsCaptureInput authorizationsCaptureInput = new AuthorizationsCaptureInput
        {
            AuthorizationId = authorizationID,
        };

        ApiResponse<CapturedPayment> result = await _paymentsController.AuthorizationsCaptureAsync(
            authorizationsCaptureInput
        );

        return result;
    }

    private async Task<dynamic> _AuthorizeOrder(string orderID)
    {
        OrdersAuthorizeInput ordersAuthorizeInput = new OrdersAuthorizeInput { Id = orderID, };

        ApiResponse<OrderAuthorizeResponse> result = await _ordersController.OrdersAuthorizeAsync(
            ordersAuthorizeInput
        );

        return result;
    }

    private async Task<dynamic> _RefundCapture(string captureID)
    {
        CapturesRefundInput capturesRefundInput = new CapturesRefundInput { CaptureId = captureID };

        ApiResponse<Refund> result = await _paymentsController.CapturesRefundAsync(
            capturesRefundInput
        );

        return result;
    }
}
