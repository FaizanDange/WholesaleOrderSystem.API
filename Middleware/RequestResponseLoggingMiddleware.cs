using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace WholesaleOrderSystem.API.Middleware
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Read request body
                string requestBody = string.Empty;
                context.Request.EnableBuffering();

                if (context.Request.ContentLength > 0)
                {
                    context.Request.Body.Position = 0;
                    using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
                    {
                        requestBody = await reader.ReadToEndAsync();
                        context.Request.Body.Position = 0;
                    }
                }

                _logger.LogInformation("Incoming request {Method} {Path}{QueryString} from {RemoteIp} | Body: {RequestBody}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Request.QueryString,
                    context.Connection.RemoteIpAddress?.ToString(),
                    string.IsNullOrEmpty(requestBody) ? "<empty>" : (requestBody.Length > 4096 ? requestBody.Substring(0, 4096) + "...(truncated)" : requestBody));

                // Capture response
                var originalBodyStream = context.Response.Body;
                using (var responseBody = new MemoryStream())
                {
                    context.Response.Body = responseBody;

                    try
                    {
                        await _next(context);
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError(ex, "Unhandled exception processing request {Method} {Path}", context.Request.Method, context.Request.Path);
                        throw;
                    }

                    context.Response.Body.Seek(0, SeekOrigin.Begin);
                    string responseText = string.Empty;
                    using (var reader = new StreamReader(context.Response.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
                    {
                        responseText = await reader.ReadToEndAsync();
                        context.Response.Body.Seek(0, SeekOrigin.Begin);
                    }

                    _logger.LogInformation("Outgoing response {StatusCode} for {Method} {Path} | Body: {ResponseBody}",
                        context.Response.StatusCode,
                        context.Request.Method,
                        context.Request.Path,
                        string.IsNullOrEmpty(responseText) ? "<empty>" : (responseText.Length > 4096 ? responseText.Substring(0, 4096) + "...(truncated)" : responseText));

                    await responseBody.CopyToAsync(originalBodyStream);
                    context.Response.Body = originalBodyStream;
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Exception in RequestResponseLoggingMiddleware");
                throw;
            }
        }
    }
}
