namespace SupplyQueryDemo.Models;

internal record ManufacturerMpn
{
    public ManufacturerMpn(string manufacturer, string mpn)
    {
        Manufacturer = manufacturer;
        Mpn = mpn;
    }

    internal string Manufacturer { get; }
    internal string Mpn { get; }
}
