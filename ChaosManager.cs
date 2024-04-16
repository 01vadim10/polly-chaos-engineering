using Polly;

namespace polly_chaos_engineering;

internal class ChaosManager(IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor) : IChaosManager
{
    private const string UserQueryParam = "user";
    private const string TestUser = "test";

    public ValueTask<double> GetInjectionRateAsync(ResilienceContext context)
    {
        if (environment.IsDevelopment())
        {
            return ValueTask.FromResult(0.05);
        }

        if (environment.IsProduction())
        {
            return ValueTask.FromResult(0.03);
        }

        return ValueTask.FromResult(0.0);
    }

    public ValueTask<bool> IsChaosEnabledAsync(ResilienceContext context)
    {
        if (environment.IsDevelopment())
        {
            return ValueTask.FromResult(true);
        }

        // This condition is demonstrative and not recommended to use in real apps.
        if (environment.IsProduction() &&
            httpContextAccessor.HttpContext is { } httpContext &&
            httpContext.Request.Query.TryGetValue(UserQueryParam, out var values) &&
            values == TestUser)
        {
            // Enable chaos for 'test' user even in production 
            return ValueTask.FromResult(true);
        }

        return ValueTask.FromResult(false);
    }
}
