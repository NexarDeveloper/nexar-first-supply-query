using CsvHelper;
using CsvHelper.Configuration;
using SupplyQueryDemo.Models;
using System.Globalization;
using System.Text;

namespace SupplyQueryDemo.Files;

internal class CsvFileParser
{
    public static (bool success, List<ManufacturerMpn>) ReadManufacturerMpnList(string fileName)
    {
        List<ManufacturerMpn> list = new();

        StringBuilder errorBuilder = new();

        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Encoding = Encoding.UTF8,
            Delimiter = ",",
            HasHeaderRecord = false
        };

        using FileStream fs = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        using (var textReader = new StreamReader(fs, Encoding.UTF8))
        using (var csv = new CsvReader(textReader, configuration))
        {
            csv.Read();
            while (csv.Read())
            {
                try
                {
                    var record = csv.GetRecord<ManufacturerMpn>();
                    list.Add(record);
                }
                catch (Exception ex)
                {
                    errorBuilder.AppendLine(ex.Message);
                }
            }
        }

        if (errorBuilder.Length > 0)
        {
            errorBuilder.Insert(0, $"Errors for {fileName}\n");

            File.WriteAllText(
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "ParseErrors.txt"),
                errorBuilder.ToString());

            return (false, null)!;
        }

        return (true, list);
    }
}
