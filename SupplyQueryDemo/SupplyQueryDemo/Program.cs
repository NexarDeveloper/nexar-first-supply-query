using SupplyQueryDemo;

const string Query = @"
query Search($mpn: String!) {
  supSearchMpn(q: $mpn, limit: 2) {
    results {
      part {
        mpn
        shortDescription
        manufacturer {
          name
        }
      }
    }
  }
}";

using HttpClient supplyClient = SupplyClient.CreateClient();

while (true)
{
    // prompt for an MPN to search
    Console.Write("Search MPN: ");
    var mpn = Console.ReadLine();
    if (string.IsNullOrEmpty(mpn))
        return;

    // populate or replace the supply token
    await supplyClient.PopulateTokenAsync();

    // run the query
    Request request = new()
    {
        Query = Query,
        Variables = new Dictionary<string, object> { { "mpn", mpn } }
    };
    Response result = await supplyClient.RunQueryAsync(request);

    // check if no results
    if (result.Data?.SupSearchMpn?.Results == null || result.Data.SupSearchMpn.Results.Count == 0)
    {
        Console.WriteLine("Sorry, no parts found");
        Console.WriteLine();
        continue;
    }

    // print the results
    foreach (var it in result.Data.SupSearchMpn.Results)
    {
        Console.WriteLine($"MPN: {it?.Part?.Mpn}");
        Console.WriteLine($"Desciption: {it?.Part?.ShortDescription}");
        Console.WriteLine($"Manufacturer: {it?.Part?.Manufacturer?.Name}");
        Console.WriteLine();
    }
}

