using System.Text.Json;

namespace IqApiNetCore.Utilities
{
    //deserialize a object;
    public static class Serialization
    {
        public static T Deserialize<T>(string data)
        {
            return JsonSerializer.Deserialize<T>(data);
        }
    }
}
