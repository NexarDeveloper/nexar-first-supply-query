using Newtonsoft.Json;
using SupplyQueryDemo.API;
using SupplyQueryDemo.Config;
using SupplyQueryDemo.Files;
using SupplyQueryDemo.Helpers;
using SupplyQueryDemo.Models;
using System.Text;

namespace SupplyQueryDemo.Demos;

internal class MultiMatchQueryDemo
{
    private readonly MultiMatchQueryDemoConfig _config;
    private readonly SupplyClient _supplyClient;

    internal MultiMatchQueryDemo(MultiMatchQueryDemoConfig config, SupplyClient supplyClient)
    {
        _config = config;
        _supplyClient = supplyClient;
    }

    internal async Task Run()
    {
        while (true)
        {
            // manually enter values (manufacturer and MPN to search)
            // or use csv file as input
            Console.WriteLine("Choose one of the following options, or nothing to go back to menu: ");
            Console.WriteLine("\t#(A) Execute query for one (Manufacturer, MPN)");
            Console.WriteLine("\t#(B) Execute queries for multiple (Manufacturer, MPN) from a CSV file");
            var option = Console.ReadLine();

            switch (option)
            {
                case "A":
                    await RunForOneInput();
                    break;
                case "B":
                    Console.WriteLine("Which query would you like to execute in batches?");
                    Console.WriteLine("\t#(1) Main query to get most information (excludes sellers and technical details)");
                    Console.WriteLine("\t#(2) Get seller details");
                    Console.WriteLine("\t#(3) Get technical details");
                    Console.WriteLine("\t#(4) All above queries");
                    
                    option = Console.ReadLine();
                    if (option == null)
                    {
                        Console.Write("Invalid option. Please try again.");
                        continue;
                    }
                    
                    await ExecuteQueriesForCsvInputFile((MultiMatchQueryDemoType) (Int32.Parse(option) -1));
                    break;
                default:
                    continue;
            }
        }
    }

    private async Task<Response?> ExecuteQuery(string query)
    {
        // run the query
        Request request = new()
        {
            Query = query
        };
        return await _supplyClient.RunQueryAsync(request);
    }

    private async Task RunForOneInput()
    {
        Console.Write("Enter manufacturer, or nothing to go back to menu: ");
        var manufacturer = Console.ReadLine();
        if (string.IsNullOrEmpty(manufacturer))
            return;

        Console.Write("Enter MPN, or nothing to go back to menu: ");
        var mpn = Console.ReadLine();
        if (string.IsNullOrEmpty(mpn))
            return;

        var manufacturerMpn = new ManufacturerMpn(manufacturer, mpn);
        var result = await ExecuteQuery(MultiMatchQueryHelper.GenerateDefaultQuery(new List<ManufacturerMpn>()
        {
            manufacturerMpn
        }, _config));

        // check if no results
        if (result?.Data?.SupMultiMatch == null || result.Data.SupMultiMatch.Count == 0)
        {
            Console.WriteLine("Sorry, no parts found");
            Console.WriteLine();
        }
    }

    private async Task ExecuteQueriesForCsvInputFile(MultiMatchQueryDemoType demoType)
    {
        var (success, csvList) = CsvFileParser.ReadManufacturerMpnList(_config.InputFile ?? throw new InvalidOperationException());

        if (success && csvList?.Count > 0)
        {
            Console.WriteLine($"Found {csvList.Count} rows. Please wait while the queries are executing.");
            switch (demoType)
            {
                case MultiMatchQueryDemoType.Main:
                    await BatchExecuteMainQuery(csvList);
                    break;
                case MultiMatchQueryDemoType.Sellers:
                    await BatchExecuteSellersQuery(csvList);
                    break;
                case MultiMatchQueryDemoType.TechnicalSpecs:
                    await BatchExecuteTechSpecsQuery(csvList);
                    break;
                case MultiMatchQueryDemoType.All:
                default:
                    await BatchExecuteAllQueries(csvList);
                    break;
            }
            Console.WriteLine("Completed");
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine("CSV file could not be loaded");
        }
    }

    private async Task BatchExecuteAllQueries(List<ManufacturerMpn> list)
    {
        StringBuilder jsonBuilder = new();
        jsonBuilder.AppendLine("{\"queryResult\": [");

        var processed = 0;
        var firstJsonAttributeAppended = false;
        
        do
        {
            var batch = list.Skip(processed).Take((int)_config.BatchSize!)?.ToList();
            if (batch?.Count > 0)
            {
                processed += batch.Count;

                var queryToExecute = MultiMatchQueryHelper.GenerateWholeQuery(batch, _config);
                Response? result;
                try
                {
                    result = await ExecuteQuery(queryToExecute);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error while processing batch: {e.Message}");
                    throw;
                }

                if (result?.Data?.SupMultiMatch == null || result.Data.SupMultiMatch.Count == 0)
                {
                    Console.WriteLine("No parts found for the current batch");
                    continue;
                }

                foreach (var multiMatchRes in result?.Data?.SupMultiMatch!)
                {
                    if (multiMatchRes.Parts == null || multiMatchRes.Parts.Count == 0)
                    {
                        Console.WriteLine("No parts found for an element in the current batch");
                        continue;
                    }

                    foreach (var part in multiMatchRes.Parts)
                    {
                        // Add a comma at the end of the previous attribute since there is more content (not in the first iteration)
                        if (firstJsonAttributeAppended)
                        {
                            jsonBuilder.Append(",");
                        }
                        
                        // 1. Main query
                        if (part.SimilarParts != null &&  part.SimilarParts.Count > _config.SimilarPartsLimit)
                        {
                            part.SimilarParts = part.SimilarParts.Take((int)_config.SimilarPartsLimit).ToList();
                        }

                        // 2. Sellers
                        if (part.Sellers == null || part.Sellers.Count == 0)
                        {
                            Console.WriteLine("No sellers found for an element in the current batch");
                            continue;
                        }

                        foreach (var seller in part.Sellers)
                        {
                            if (seller.Offers == null || seller.Offers.Count == 0)
                            {
                                Console.WriteLine("Skipping empty offers");
                                continue;
                            }

                            foreach (var offer in seller.Offers)
                            {
                                if (offer.Prices == null || offer.Prices.Count == 0)
                                {
                                    Console.WriteLine("Skipping empty prices");
                                    continue;
                                }

                                OfferPrice priceWithGreatestQuantity = offer.Prices.Aggregate((p1,p2) => p1.Quantity > p2.Quantity ? p1 : p2);
                                offer.Prices = offer.Prices.Where(p => p.Quantity == priceWithGreatestQuantity?.Quantity)?.ToList();
                            }
                        }
                        
                        // 3. Technical details
                        // (included in specs already as long as no filtering takes place)
                        
                        // Update json
                        var jsonSerializationOptions = new JsonSerializerSettings();
                        bool includeNullValues = _config.IncludeNullValues ?? true;
                        jsonSerializationOptions.NullValueHandling = includeNullValues ? NullValueHandling.Include : NullValueHandling.Ignore;
                        string json = JsonConvert.SerializeObject(part, Formatting.Indented, jsonSerializationOptions);
                        jsonBuilder.AppendLine(json);
                        
                        if (!firstJsonAttributeAppended)
                            firstJsonAttributeAppended = true;
                    }
                }
            }
        }
        while (processed < list.Count);

        jsonBuilder.Append("]}");
        JsonFileHandler.SaveToJsonFile(jsonBuilder.ToString(), Path.Combine(_config.OutputDirectory ?? throw new ArgumentNullException(), "multimatch_query.json"));
    }

    private async Task BatchExecuteMainQuery(List<ManufacturerMpn> list)
    {
        StringBuilder jsonBuilder = new();
        jsonBuilder.AppendLine("{\"mainQueryResult\": [");

        int processed = 0;
        var firstJsonAttributeAppended = false;
        
        do
        {
            var batch = list.Skip(processed).Take((int)_config.BatchSize!)?.ToList();
            if (batch?.Count > 0)
            {
                processed += batch.Count;

                var queryToExecute = MultiMatchQueryHelper.GenerateDefaultQuery(batch, _config);
                Response? result;
                try
                {
                    result = await ExecuteQuery(queryToExecute);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error while processing batch: {e.Message}");
                    throw;
                }

                if (result?.Data?.SupMultiMatch == null || result.Data.SupMultiMatch.Count == 0)
                {
                    Console.WriteLine("No parts found for the current batch");
                    continue;
                }

                foreach (var multiMatchRes in result?.Data?.SupMultiMatch!)
                {
                    if (multiMatchRes.Parts == null || multiMatchRes.Parts.Count == 0)
                    {
                        Console.WriteLine("No parts found for an element in the current batch");
                        continue;
                    }

                    foreach (var part in multiMatchRes.Parts)
                    {
                        // Add a comma at the end of the previous attribute since there is more content (not in the first iteration)
                        if (firstJsonAttributeAppended)
                        {
                            jsonBuilder.Append(",");
                        }
                        
                        // Suggest N similar parts
                        if (part.SimilarParts != null &&  part.SimilarParts.Count > _config.SimilarPartsLimit)
                        {
                            part.SimilarParts = part.SimilarParts.Take((int)_config.SimilarPartsLimit).ToList();
                        }

                        // For the default query, limit the specs to those specified in appsettings.json only
                        if (_config.SpecsToFetch != null)
                        {
                            if (part.Specs is { Count: > 0 })
                            {
                                part.Specs = part.Specs.Where(s => _config.SpecsToFetch.Contains(s.Attribute?.ShortName))?.ToList();
                            }
                        }
                        
                        // Update json
                        var jsonSerializationOptions = new JsonSerializerSettings();
                        bool includeNullValues = _config.IncludeNullValues ?? true;
                        jsonSerializationOptions.NullValueHandling = includeNullValues ? NullValueHandling.Include : NullValueHandling.Ignore;
                        string json = JsonConvert.SerializeObject(part, Formatting.Indented, jsonSerializationOptions);
                        jsonBuilder.AppendLine(json);
                        
                        if (!firstJsonAttributeAppended)
                            firstJsonAttributeAppended = true;
                    }
                }
            }
        }
        while (processed < list.Count);

        jsonBuilder.Append("]}");
        JsonFileHandler.SaveToJsonFile(jsonBuilder.ToString(), Path.Combine(_config.OutputDirectory ?? throw new ArgumentNullException(), "multimatch_main_query.json"));
    }
    
    private async Task BatchExecuteSellersQuery(List<ManufacturerMpn> list)
    {
        StringBuilder jsonBuilder = new();
        jsonBuilder.AppendLine("{\"sellersQueryResult\": [");

        int processed = 0;
        var firstJsonAttributeAppended = false;
        
        do
        {
            var batch = list.Skip(processed).Take((int)_config.BatchSize!)?.ToList();
            if (batch?.Count > 0)
            {
                processed += batch.Count;

                var queryToExecute = MultiMatchQueryHelper.GenerateSellersQuery(batch, _config);
                Response? result;
                try
                {
                    result = await ExecuteQuery(queryToExecute);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error while processing batch: {e.Message}");
                    throw;
                }

                if (result?.Data?.SupMultiMatch == null || result.Data.SupMultiMatch.Count == 0)
                {
                    Console.WriteLine("No parts found for the current batch");
                    continue;
                }

                foreach (var multiMatchRes in result?.Data?.SupMultiMatch!)
                {
                    if (multiMatchRes.Parts == null || multiMatchRes.Parts.Count == 0)
                    {
                        Console.WriteLine("No parts found for an element in the current batch");
                        continue;
                    }

                    foreach (var part in multiMatchRes.Parts)
                    {
                        // Add a comma at the end of the previous attribute since there is more content (not in the first iteration)
                        if (firstJsonAttributeAppended)
                        {
                            jsonBuilder.Append(",");
                        }
                        
                        if (part.Sellers == null || part.Sellers.Count == 0)
                        {
                            Console.WriteLine("No sellers found for an element in the current batch");
                            continue;
                        }

                        foreach (var seller in part.Sellers)
                        {
                            if (seller.Offers == null || seller.Offers.Count == 0)
                            {
                                Console.WriteLine("Skipping empty offers");
                                continue;
                            }

                            foreach (var offer in seller.Offers)
                            {
                                if (offer.Prices == null || offer.Prices.Count == 0)
                                {
                                    Console.WriteLine("Skipping empty prices");
                                    continue;
                                }

                                // Choose the best deal (i.e. price offering with the biggest quantity)
                                OfferPrice priceWithGreatestQuantity = offer.Prices.Aggregate((p1,p2) => p1.Quantity > p2.Quantity ? p1 : p2);
                                offer.Prices = offer.Prices.Where(p => p.Quantity == priceWithGreatestQuantity?.Quantity)?.ToList();
                            }
                        }

                        // Update json
                        var jsonSerializationOptions = new JsonSerializerSettings();
                        bool includeNullValues = _config.IncludeNullValues ?? true;
                        jsonSerializationOptions.NullValueHandling = includeNullValues ? NullValueHandling.Include : NullValueHandling.Ignore;
                        string json = JsonConvert.SerializeObject(part, Formatting.Indented, jsonSerializationOptions);
                        jsonBuilder.AppendLine(json);
                        
                        if (!firstJsonAttributeAppended)
                            firstJsonAttributeAppended = true;
                    }
                }
            }
        }
        while (processed < list.Count);

        jsonBuilder.Append("]}");
        JsonFileHandler.SaveToJsonFile(jsonBuilder.ToString(), Path.Combine(_config.OutputDirectory ?? throw new ArgumentNullException(), "multimatch_sellers_query.json"));
    }
    
    private async Task BatchExecuteTechSpecsQuery(List<ManufacturerMpn> list)
    {
        StringBuilder jsonBuilder = new();
        jsonBuilder.AppendLine("{\"techSpecsQueryResult\": [");

        int processed = 0;
        var firstJsonAttributeAppended = false;
        
        do
        {
            var batch = list.Skip(processed).Take((int)_config.BatchSize!)?.ToList();
            if (batch?.Count > 0)
            {
                processed += batch.Count;

                var queryToExecute = MultiMatchQueryHelper.GenerateTechnicalDetailsQuery(batch, _config);
                Response? result;
                try
                {
                    result = await ExecuteQuery(queryToExecute);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error while processing batch: {e.Message}");
                    throw;
                }

                if (result?.Data?.SupMultiMatch == null || result.Data.SupMultiMatch.Count == 0)
                {
                    Console.WriteLine("No parts found for the current batch");
                    continue;
                }

                foreach (var multiMatchRes in result?.Data?.SupMultiMatch!)
                {
                    if (multiMatchRes.Parts == null || multiMatchRes.Parts.Count == 0)
                    {
                        Console.WriteLine("No parts found for an element in the current batch");
                        continue;
                    }

                    foreach (var part in multiMatchRes.Parts)
                    {
                        // Add a comma at the end of the previous attribute since there is more content (not in the first iteration)
                        if (firstJsonAttributeAppended)
                        {
                            jsonBuilder.Append(",");
                        }
                        
                        // Update json
                        var jsonSerializationOptions = new JsonSerializerSettings();
                        bool includeNullValues = _config.IncludeNullValues ?? true;
                        jsonSerializationOptions.NullValueHandling = includeNullValues ? NullValueHandling.Include : NullValueHandling.Ignore;
                        string json = JsonConvert.SerializeObject(part, Formatting.Indented, jsonSerializationOptions);
                        jsonBuilder.AppendLine(json);
                        
                        if (!firstJsonAttributeAppended)
                            firstJsonAttributeAppended = true;
                    }
                }
            }
        }
        while (processed < list.Count);

        jsonBuilder.Append("]}");
        JsonFileHandler.SaveToJsonFile(jsonBuilder.ToString(), Path.Combine(_config.OutputDirectory ?? throw new ArgumentNullException(), "multimatch_tech_specs_query.json"));
    }
}