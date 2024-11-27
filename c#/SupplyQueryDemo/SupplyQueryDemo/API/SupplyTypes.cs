using System.Text.Json.Serialization;

namespace SupplyQueryDemo.API;

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

    [JsonPropertyName("supMultiMatch")]
    public List<SupMultiMatch>? SupMultiMatch { get; set; }
}

internal class SupSearchMpn
{
    [JsonPropertyName("results")]
    public List<Result>? Results { get; set; }
}

internal class SupMultiMatch
{
    [JsonPropertyName("parts")]
    public List<Part>? Parts { get; set; }
}

internal class Result
{
    [JsonPropertyName("part")]
    public Part? Part { get; set; }
}

internal class Part
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("mpn")]
    public string? Mpn { get; set; }

    [JsonPropertyName("shortDescription")]
    public string? ShortDescription { get; set; }

    [JsonPropertyName("manufacturer")]
    public Manufacturer? Manufacturer { get; set; }
    
    [JsonPropertyName("category")]
    public Category? Category { get; set; }
    
    [JsonPropertyName("medianPrice1000")]
    public MedianPrice1000? MedianPrice1000 { get; set; }
    
    [JsonPropertyName("similarParts")]
    public List<Part>? SimilarParts { get; set; }
    
    [JsonPropertyName("bestDatasheet")]
    public BestDatasheet? BestDatasheet { get; set; }

    [JsonPropertyName("specs")]
    public List<Spec>? Specs { get; set; }
    
    [JsonPropertyName("estimatedFactoryLeadDays")]
    public int? EstimatedFactoryLeadDays { get; set; }
    
    [JsonPropertyName("sellers")]
    public List<Seller>? Sellers { get; set; }
}

internal class Manufacturer
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("homepageUrl")]
    public string? HomepageUrl { get; set; }
}

internal class Category
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

internal class MedianPrice1000
{
    [JsonPropertyName("price")]
    public float? Price { get; set; }
    
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }
}

internal class BestDatasheet
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("url")]
    public string? Url { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }
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

internal class Seller
{
    [JsonPropertyName("company")]
    public Company? Company { get; set; }
    
    [JsonPropertyName("country")]
    public string? Country { get; set; }
    
    [JsonPropertyName("offers")]
    public List<Offer>? Offers { get; set; }
}

internal class Company
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("homepageUrl")]
    public string? HompepageUrl { get; set; }
}

internal class Offer
{
    [JsonPropertyName("factoryLeadDays")]
    public int? FactoryLeadDays { get; set; }
    
    [JsonPropertyName("factoryPackQuantity")]
    public int? FactoryPackQuantity { get; set; }
    
    [JsonPropertyName("inventoryLevel")]
    public int? InventoryLevel { get; set; }
    
    [JsonPropertyName("moq")]
    public int? Moq { get; set; }
    
    [JsonPropertyName("packaging")]
    public string? Packaging { get; set; }
    
    [JsonPropertyName("prices")]
    public List<OfferPrice>? Prices { get; set; }
}

internal class OfferPrice
{
    [JsonPropertyName("price")]
    public float? Price { get; set; }
    
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }
    
    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }
}
