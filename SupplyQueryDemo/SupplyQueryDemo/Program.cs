using SupplyQueryDemo;

const string QueryTemplate = @"
query Search {{
  supSearchMpn(q: ""{0}"", limit: 2) {{
    results {{
      part {{
        mpn
        shortDescription
        manufacturer {{
          name
        }}
      }}
    }}
  }}
}}";

while (true)
{
    // prompt for an MPN to search
    Console.Write("Search MPN: ");
    var mpn = Console.ReadLine();
    if (string.IsNullOrEmpty(mpn))
        return;

    using HttpClient supplyClient = await SupplyClient.GetClientAsync();

    // substitute the MPN into the query
    string query = string.Format(QueryTemplate, mpn);

    // run the query
    Request request = new() { Query = query };
    Response result = await supplyClient.RunQueryAsync(request);

    if (result.Data?.SupSearchMpn?.Results == null)
    {
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

