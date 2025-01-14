using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Net.Http;
using Yarp.ReverseProxy.Forwarder;

public class CustomForwarderHttpClientFactory : IForwarderHttpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CustomForwarderHttpClientFactory> _logger;

    public CustomForwarderHttpClientFactory(IHttpClientFactory httpClientFactory, ILogger<CustomForwarderHttpClientFactory> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context)
    {

        
        // Create a handler chain with Polly using retry and circuit breaker
        var handler = new HttpClientHandler();

        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(2, TimeSpan.FromMinutes(1),
            onBreak: (result, timespan) =>
            {
                _logger.LogError($"Circuit broken! Duration: {timespan.TotalSeconds} seconds.");
            },
            onReset: () =>
            {
                _logger.LogInformation("Circuit reset at " + DateTime.UtcNow);
            },
            onHalfOpen: () =>
            {
                _logger.LogInformation("Circuit in half-open state at " + DateTime.UtcNow);
            });

        // Combine the policies
        var policyHandler = new PolicyHttpMessageHandler(retryPolicy);
        policyHandler.InnerHandler = new PolicyHttpMessageHandler(circuitBreakerPolicy)
        {
            InnerHandler = handler
        };

        // Create HttpMessageInvoker
        return new HttpMessageInvoker(policyHandler);
    }
}
