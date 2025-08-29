namespace SGServer.Middleware
{
    public class ApiKeyAuthMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyAuthMiddleware> logger)
    {
        private const string ApiKeyHeaderName = "X-API-Key";

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                // Not an API route, continue without authentication
                await next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
            {
                logger.LogWarning("API Key missing in request");
                context.Response.StatusCode = 401; // Unauthorized
                await context.Response.WriteAsync("API Key is required");
                return;
            }

            var apiKey = configuration.GetValue<string>("ApiKey");
            
            if (apiKey != null && !apiKey.Equals(extractedApiKey))
            {
                logger.LogWarning("Invalid API Key provided");
                context.Response.StatusCode = 401; // Unauthorized
                await context.Response.WriteAsync("Invalid API Key");
                return;
            }

            logger.LogInformation("Valid API Key provided");
            await next(context);
        }
    }
}
