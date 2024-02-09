using System.Text.Json.Serialization;

namespace SupplyQueryDemo
{
    internal class Request
    {
        [JsonPropertyName("query")]
        public string? Query { get; set; }

        [JsonPropertyName("variables")]
        public Dictionary<string, object>? Variables { get; set; }
    }

    internal class Response
    {
        [JsonPropertyName("data")]
        public Data? Data { get; set; }
    }

    internal class Data
    {
        [JsonPropertyName("supSearchMpn")]
        public SupSearchMpn? SupSearchMpn { get; set; }
    }

    internal class SupSearchMpn
    {
        [JsonPropertyName("results")]
        public List<Result>? Results { get; set; }
    }

    internal class Result
    {
        [JsonPropertyName("part")]
        public Part? Part { get; set; }
    }

    internal class Part
    {
        [JsonPropertyName("mpn")]
        public string? Mpn { get; set; }

        [JsonPropertyName("shortDescription")]
        public string? ShortDescription { get; set; }

        [JsonPropertyName("manufacturer")]
        public Manufacturer? Manufacturer { get; set; }

        [JsonPropertyName("specs")]
        public List<Spec>? Specs { get; set; }
    }

    internal class Manufacturer
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    internal class Spec
    {
        [JsonPropertyName("attribute")]
        public Attribute? Attribute { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }

    internal class Attribute
    {
        [JsonPropertyName("shortname")]
        public string? ShortName { get; set; }
    }
}
