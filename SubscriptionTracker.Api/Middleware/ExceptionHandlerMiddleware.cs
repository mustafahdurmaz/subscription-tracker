// Global exception handler. Pipeline'in en başına bağlanır.
// Tüm beklenmeyen hataları yakalar, tek tip JSON response döner (500 + error mesajı).
// Service ve controller'ların try-catch'le dolmamasını sağlar.
namespace SubscriptionTracker.Api.Middleware;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Hata log'a yazılır (ileride Application Insights / Seq vb. tutar).
            _logger.LogError(ex, "Beklenmeyen hata: {Path}", context.Request.Path);

            // Eğer response zaten gönderilmeye başladıysa headerlara dokunamayız.
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            // Sunum/dev için detail dahil — production'da kaldırılır.
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Sunucuda beklenmeyen bir hata oluştu.",
                detail = ex.Message
            });
        }
    }
}
