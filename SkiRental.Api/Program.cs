using Microsoft.EntityFrameworkCore;
using SkiRental.Domain.Enums;
using SkiRental.Logic;
using SkiRental.Logic.Interfaces;
using SkiRental.Logic.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSkiRentalServices(
    builder.Configuration.GetConnectionString("SkiRentalSqlite")
        ?? "Data Source=SkiRental.db",
    configuration: builder.Configuration);

builder.Services.AddHttpClient(nameof(PayUPaymentProvider));
builder.Services.AddScoped<IPaymentProvider>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var http = factory.CreateClient(nameof(PayUPaymentProvider));
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PayUOptions>>();
    var logger = sp.GetRequiredService<ILogger<PayUPaymentProvider>>();
    return new PayUPaymentProvider(http, options, logger);
});
builder.Services.Configure<PayUOptions>(builder.Configuration.GetSection(PayUOptions.SectionName));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SkiRentalDbContext>();
    await db.Database.MigrateAsync();
}

app.MapPost("/api/payu/notify", async (HttpRequest request, IRentalRepository rentals, ILogger<Program> log) =>
{
    var body = await new StreamReader(request.Body).ReadToEndAsync();
    log.LogInformation("PayU notify received: {Body}", body);

    // PayU sends orderId in notification — update rental when COMPLETED
    if (body.Contains("COMPLETED", StringComparison.OrdinalIgnoreCase))
    {
        var active = await rentals.GetActiveRentalsAsync();
        foreach (var r in active.Where(x => x.PaymentStatus == PaymentStatus.Preauthorized))
        {
            r.PaymentStatus = PaymentStatus.Paid;
            await rentals.UpdateAsync(r);
        }
    }

    return Results.Ok();
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "SkiRental.Api" }));

app.Run();
