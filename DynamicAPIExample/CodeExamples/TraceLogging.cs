using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace DynamicCommand
{

    public class NunitTestContextLogger : ILogger
    {
        private readonly string _name;

        public NunitTestContextLogger(string name)
        {
            _name = name;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            TestContext.WriteLine($"{logLevel.ToString()} - {eventId.Id} - {_name} - {formatter(state, exception)}");
        }
    }

    public class NunitTestContextLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, NunitTestContextLogger> _loggers = new ConcurrentDictionary<string, NunitTestContextLogger>();

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new NunitTestContextLogger(name));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }

    public class CustomLoggingScopeHttpMessageHandler : DelegatingHandler
    {
        private readonly ILogger<CustomLoggingScopeHttpMessageHandler> _logger;

        public CustomLoggingScopeHttpMessageHandler(ILogger<CustomLoggingScopeHttpMessageHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private static readonly UTF8Encoding Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }


            if (request.Content != null)
            {
                var requestBodyBytes = await request.Content.ReadAsByteArrayAsync();
                var requestBodyString = Encoding.GetString(requestBodyBytes);
                _logger.LogInformation("Request body: \n{Body}", requestBodyString);
            }
            else
            {
                _logger.LogInformation("No request body");
            }


            var response = await base.SendAsync(request, cancellationToken);
            if (response.Content != null)
            {
                var responseBytes = await response.Content.ReadAsByteArrayAsync();
                var responseString = Encoding.GetString(responseBytes);
                _logger.LogInformation("Response body: \n{Body}", responseString);
            }
            else
            {
                _logger.LogInformation("No response body");
            }

            return response;
        }
    }

}
