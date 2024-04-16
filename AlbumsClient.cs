using System.Text.Json.Serialization;

namespace polly_chaos_engineering;

public class AlbumsClient(HttpClient client)
{
    public async Task<IEnumerable<Album>> GetAlbumsAsync(CancellationToken cancellationToken)
        => await client.GetFromJsonAsync<IEnumerable<Album>>("/albums", cancellationToken) ?? [];
}

public record Album(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("userId")] int UserId,
    [property: JsonPropertyName("title")] string Title);