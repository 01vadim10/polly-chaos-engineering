using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Simmy;
using Polly.Simmy.Fault;
using Polly.Simmy.Latency;
using Polly.Simmy.Outcomes;
using polly_chaos_engineering;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.TryAddSingleton<IChaosManager, ChaosManager>();
services.AddHttpContextAccessor();

var httpClientBuilder = builder.Services.AddHttpClient<AlbumsClient>(client =>
    client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com"));

httpClientBuilder
    .AddStandardResilienceHandler()
    .Configure(options =>
    {
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(1);

        options.CircuitBreaker.ShouldHandle = args => args.Outcome switch
        {
            { } outcome when HttpClientResiliencePredicates.IsTransient(outcome) => PredicateResult.True(),
            { Exception: IndexOutOfRangeException } => PredicateResult.True(),
            _ => PredicateResult.False()
        };

        options.Retry.ShouldHandle = args => args.Outcome switch
        {
            { } outcome when HttpClientResiliencePredicates.IsTransient(outcome) => PredicateResult.True(),
            { Exception: IndexOutOfRangeException } => PredicateResult.True(),
            _ => PredicateResult.False()
        };
    });

httpClientBuilder.AddResilienceHandler("chaos", (builder, context) =>
{
    var chaosManager = context.ServiceProvider.GetRequiredService<IChaosManager>();

    builder
        .AddChaosLatency(new ChaosLatencyStrategyOptions
        {
            EnabledGenerator = args => chaosManager.IsChaosEnabledAsync(args.Context),
            InjectionRateGenerator = args => chaosManager.GetInjectionRateAsync(args.Context),
            Latency = TimeSpan.FromSeconds(5)
        })
        .AddChaosFault(new ChaosFaultStrategyOptions
        {
            EnabledGenerator = args => chaosManager.IsChaosEnabledAsync(args.Context),
            InjectionRateGenerator = args => chaosManager.GetInjectionRateAsync(args.Context),
            FaultGenerator = new FaultGenerator().AddException(() => new IndexOutOfRangeException("Chaos Fault injection!"))
        })
        .AddChaosOutcome(new ChaosOutcomeStrategyOptions<HttpResponseMessage>
        {
            EnabledGenerator = args => chaosManager.IsChaosEnabledAsync(args.Context),
            InjectionRateGenerator = args => chaosManager.GetInjectionRateAsync(args.Context),
            OutcomeGenerator = new OutcomeGenerator<HttpResponseMessage>().AddResult(() => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError))
        });
});

var app = builder.Build();
app.MapGet("/", (AlbumsClient client, CancellationToken cancellationToken) => client.GetAlbumsAsync(cancellationToken));
app.Run();