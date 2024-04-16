using Polly;

namespace polly_chaos_engineering;

public interface IChaosManager
{
    ValueTask<bool> IsChaosEnabledAsync(ResilienceContext context);
    ValueTask<double> GetInjectionRateAsync(ResilienceContext context);
}
