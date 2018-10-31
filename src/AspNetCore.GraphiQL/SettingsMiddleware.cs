using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore
{
    internal sealed class SettingsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly GraphiQLOptions _options;

        public SettingsMiddleware(RequestDelegate next, GraphiQLOptions options)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.ContentType = "application/javascript";
            await context.Response.WriteAsync("window.Settings = {", context.RequestAborted);
            await context.Response.WriteAsync("url: \"{http://localhost:5000/}\",", context.RequestAborted);
            await context.Response.WriteAsync("subscriptionUrl: \"ws://localhost:5000/subscriptions\"", context.RequestAborted);
            await context.Response.WriteAsync("};", context.RequestAborted);
        }
    }
}
