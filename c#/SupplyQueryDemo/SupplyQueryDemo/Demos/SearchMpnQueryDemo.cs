using SupplyQueryDemo.API;

namespace SupplyQueryDemo.Demos;

internal class SearchMpnQueryDemo
{
    private const string Query = @"
                        query Search($mpn: String!) {
                          supSearchMpn(q: $mpn, limit: 2) {
                            results {
                              part {
                                mpn
                                shortDescription
                                manufacturer {
                                  name
                                }
                                specs {
                                  attribute {
                                    shortname
                                  }
                                  value
                                }
                              }
                            }
                          }
                        }";

    internal static async Task Run(SupplyClient supplyClient)
    {
        while (true)
        {
            // prompt for an MPN to search
            Console.Write("Enter MPN, or nothing to go back to menu: ");
            var mpn = Console.ReadLine();
            if (string.IsNullOrEmpty(mpn))
                return;

            // run the query
            Request request = new()
            {
                Query = Query,
                Variables = new Dictionary<string, object>
                {
                    {
                        "mpn", mpn
                    }
                }
            };
            Response? result = await supplyClient.RunQueryAsync(request);

            // check if no results
            if (result?.Data?.SupSearchMpn?.Results == null || result.Data.SupSearchMpn.Results.Count == 0)
            {
                Console.WriteLine("Sorry, no parts found");
                Console.WriteLine();
                continue;
            }

            // get lifecycle status
            string GetLifecycleStatus(List<Spec>? specs)
            {
                Spec? spec = specs?.FirstOrDefault(x => x.Attribute?.ShortName == "lifecyclestatus");
                return spec?.Value ?? string.Empty;
            }

            // print the results
            foreach (var it in result.Data.SupSearchMpn.Results)
            {
                Console.WriteLine($"MPN: {it?.Part?.Mpn}");
                Console.WriteLine($"Description: {it?.Part?.ShortDescription}");
                Console.WriteLine($"Manufacturer: {it?.Part?.Manufacturer?.Name}");
                Console.WriteLine($"Lifecycle Status: {GetLifecycleStatus(it?.Part?.Specs)}");
                Console.WriteLine();
            }
        }
    }
}
