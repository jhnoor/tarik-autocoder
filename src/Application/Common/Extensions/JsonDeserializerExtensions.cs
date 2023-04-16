using System.Text.Json;

namespace Tarik.Application.Common;

public static class DeserializeExtensions
{
    private static JsonSerializerOptions defaultSerializerSettings = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    public static T? Deserialize<T>(this string json)
    {
        return JsonSerializer.Deserialize<T>(json, defaultSerializerSettings);
    }
}
