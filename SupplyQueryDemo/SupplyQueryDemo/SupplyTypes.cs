using Newtonsoft.Json;

namespace SupplyQueryDemo
{
    internal class Request
    {
        [JsonProperty("query")]
        public string? Query { get; set; }

        [JsonProperty("variables")]
        public Dictionary<string, object>? Variables { get; set; }
    }

    internal class Response
    {
        [JsonProperty("data")]
        public Data? Data { get; set; }
    }

    internal class Data
    {
        [JsonProperty("supSearchMpn")]
        public SupSearchMpn? SupSearchMpn { get; set; }
    }

    internal class SupSearchMpn
    {
        [JsonProperty("results")]
        public List<Result>? Results { get; set; }
    }

    internal class Result
    {
        [JsonProperty("part")]
        public Part? Part { get; set; }
    }

    internal class Part
    {
        [JsonProperty("mpn")]
        public string? Mpn { get; set; }

        [JsonProperty("shortDescription")]
        public string? ShortDescription { get; set; }

        [JsonProperty("manufacturer")]
        public Manufacturer? Manufacturer { get; set; }
    }

    internal class Manufacturer
    {
        [JsonProperty("name")]
        public string? Name { get; set; }
    }
}
