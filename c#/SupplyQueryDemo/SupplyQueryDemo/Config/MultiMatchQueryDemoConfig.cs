namespace SupplyQueryDemo.Config;

public class MultiMatchQueryDemoConfig
{
    public string? InputFile { get; init; }
    public string? OutputDirectory { get; init; }
    public bool? RequireAuthorizedSellers { get; init; }
    public int? SimilarPartsLimit { get; init; }
    public string[]? SpecsToFetch { get; init; }
    public int? BatchSize { get; init; }
    public bool? IncludeNullValues { get; init; }
}
