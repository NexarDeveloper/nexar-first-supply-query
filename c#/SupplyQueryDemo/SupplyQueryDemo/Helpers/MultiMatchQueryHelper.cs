using SupplyQueryDemo.Config;
using SupplyQueryDemo.Models;
using System.Text;

namespace SupplyQueryDemo.Helpers;

public static class MultiMatchQueryHelper
{
  private const string WholeQuery = @"
                        query MultiMatchMainQuery {
                          supMultiMatch(
                            queries: [<QUERIES_PLACEHOLDER>]
                            options: { requireAuthorizedSellers: <REQUIRE_AUTHORIZED_SELLERS_PLACEHOLDER> }
                          ) {
                            parts {
                              mpn
                              manufacturer {
                                name
                                homepageUrl
                              }
                              category {
                                name
                              }
                              shortDescription
                              medianPrice1000 {
                                price
                                currency
                              }
                              similarParts {
                                name
                                shortDescription
                                manufacturer {
                                  name
                                }
                              }
                              bestDatasheet {
                                name
                                url
                                createdAt
                              }
                              estimatedFactoryLeadDays
                              specs {
                                displayValue
                                siValue
                                units
                                unitsName
                                unitsSymbol
                                value
                                valueType
                                attribute {
                                  name
                                  group
                                  id
                                  shortname
                                  unitsName
                                  unitsSymbol
                                  valueType
                                }
                              }
                              sellers {
                                offers {
                                  factoryLeadDays
                                  packaging
                                  prices {
                                    currency
                                    price
                                    quantity
                                  }
                                  factoryPackQuantity
                                  inventoryLevel
                                  moq
                                }
                                company {
                                  homepageUrl
                                  name
                                }
                                country
                              }
                            }
                          }
                        }";
  
    private const string DefaultQuery = @"
                        query MultiMatchMainQuery {
                          supMultiMatch(
                            queries: [<QUERIES_PLACEHOLDER>]
                            options: { requireAuthorizedSellers: <REQUIRE_AUTHORIZED_SELLERS_PLACEHOLDER> }
                          ) {
                            parts {
                              mpn
                              manufacturer {
                                name
                                homepageUrl
                              }
                              category {
                                name
                              }
                              shortDescription
                              medianPrice1000 {
                                price
                                currency
                              }
                              similarParts {
                                name
                                shortDescription
                                manufacturer {
                                  name
                                }
                              }
                              bestDatasheet {
                                name
                                url
                                createdAt
                              }
                              estimatedFactoryLeadDays
                              specs {
                                value
                                attribute {
                                  shortname
                                }
                              }
                            }
                          }
                        }";

    private const string SellersQuery = @"
                        query MultiMatchSellersQuery {
                          supMultiMatch(
                            queries: [<QUERIES_PLACEHOLDER>]
                            options: { requireAuthorizedSellers: <REQUIRE_AUTHORIZED_SELLERS_PLACEHOLDER> }
                          ) {
                            parts {
                              mpn
                              manufacturer {
                                name
                              }
                              sellers {
                                offers {
                                  factoryLeadDays
                                  packaging
                                  prices {
                                    currency
                                    price
                                    quantity
                                  }
                                  factoryPackQuantity
                                  inventoryLevel
                                  moq
                                }
                                company {
                                  homepageUrl
                                  name
                                }
                                country
                              }
                            }
                          }
                        }";

    private const string TechnicalDetailsQuery = @"
                        query MultiMatchTechSpecsQuery {
                          supMultiMatch(
                            queries: [<QUERIES_PLACEHOLDER>]
                            options: { requireAuthorizedSellers: <REQUIRE_AUTHORIZED_SELLERS_PLACEHOLDER> }
                          ) {
                            parts {
                              manufacturer {
                                name
                              }
                              mpn
                              specs {
                                displayValue
                                siValue
                                units
                                unitsName
                                unitsSymbol
                                value
                                valueType
                                attribute {
                                  name
                                  group
                                  id
                                  shortname
                                  unitsName
                                  unitsSymbol
                                  valueType
                                }
                              }
                            }
                          }
                        }";

    internal static string GenerateWholeQuery(IEnumerable<ManufacturerMpn>? list, MultiMatchQueryDemoConfig config)
    {
      return GenerateQuery(list, WholeQuery, config);
    }
    
    internal static string GenerateDefaultQuery(IEnumerable<ManufacturerMpn>? list, MultiMatchQueryDemoConfig config)
    {
      return GenerateQuery(list, DefaultQuery, config);
    }
    
    internal static string GenerateSellersQuery(IEnumerable<ManufacturerMpn>? list, MultiMatchQueryDemoConfig config)
    {
      return GenerateQuery(list, SellersQuery, config);
    }
    
    internal static string GenerateTechnicalDetailsQuery(IEnumerable<ManufacturerMpn>? list, MultiMatchQueryDemoConfig config)
    {
      return GenerateQuery(list, TechnicalDetailsQuery, config);
    }

    private static string GenerateQuery(IEnumerable<ManufacturerMpn>? list, string queryToComplete, MultiMatchQueryDemoConfig config)
    {
      if (list?.Any() != true)
        throw new ArgumentNullException(nameof(list));

      bool requireAuthorizedSellers = config.RequireAuthorizedSellers ?? false;
      queryToComplete = queryToComplete.Replace("<REQUIRE_AUTHORIZED_SELLERS_PLACEHOLDER>", requireAuthorizedSellers.ToString().ToLower());
      
      const string queryParamsFormat = "{ manufacturer: \"<MANUFACTURER_PLACEHOLDER>\", mpn: \"<MPN_PLACEHOLDER>\" }";
      
      StringBuilder sb = new StringBuilder();
      foreach(var p in list)
      {
        string queryParams = queryParamsFormat.Replace("<MANUFACTURER_PLACEHOLDER>", CleanParameter(p.Manufacturer))
          .Replace("<MPN_PLACEHOLDER>", CleanParameter(p.Mpn));
        sb.AppendLine(queryParams);
      }
      
      string finalQueryParams = sb.ToString();
      return queryToComplete.Replace("<QUERIES_PLACEHOLDER>", finalQueryParams);
    }

    private static string CleanParameter(string parameter)
    {
      // Remove any character that is not allowed
      return parameter.Replace("\"", String.Empty);
    }
}
