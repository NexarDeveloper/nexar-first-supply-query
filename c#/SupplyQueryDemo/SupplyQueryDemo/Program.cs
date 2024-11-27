using SupplyQueryDemo.API;
using SupplyQueryDemo.Demos;
using Microsoft.Extensions.Configuration;
using SupplyQueryDemo.Config;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false);

IConfiguration config = builder.Build();
var multimatchConfig = config.GetSection("MultiMatchQueryDemo").Get<MultiMatchQueryDemoConfig>();

// assume Nexar client ID and secret are set as environment variables
var clientId = Environment.GetEnvironmentVariable("NEXAR_CLIENT_ID") ?? throw new InvalidOperationException("Please set environment variable 'NEXAR_CLIENT_ID'");
var clientSecret = Environment.GetEnvironmentVariable("NEXAR_CLIENT_SECRET") ?? throw new InvalidOperationException("Please set environment variable 'NEXAR_CLIENT_SECRET'");
using SupplyClient supplyClient = new(clientId, clientSecret);

MultiMatchQueryDemo? multiMatchQueryDemo = null;
if (multimatchConfig != null)
{
    multiMatchQueryDemo = new MultiMatchQueryDemo(multimatchConfig, supplyClient);
}

while (true)
{
    // prompt user to choose an option
    Console.WriteLine("Please choose from the following options: ");
    Console.WriteLine("\t(1) Search MPN");
    Console.WriteLine("\t(2) Multi match");
    
    var option = Console.ReadLine();
    switch (option)
    {
        case "1":
            await SearchMpnQueryDemo.Run(supplyClient);
            break;
        case "2":
            await multiMatchQueryDemo?.Run()!;
            break;
    }
}