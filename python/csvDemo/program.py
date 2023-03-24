import os, csv
from nexarClient import NexarClient

mpns = []

with open("input.csv", newline="") as inputFile:
    data = csv.reader(inputFile, delimiter=" ", quotechar="|")
    for row in data:
        mpns += row[0].split(",")

gqlQuery = '''
query csvDemo ($queries: [SupPartMatchQuery!]!) {
  supMultiMatch (
    currency: "EUR",
    queries: $queries
  ){
    parts {
      mpn
      name
      sellers {
        company {
          id
          name
        }
        offers {
          inventoryLevel
          prices {
            quantity
            convertedPrice
            convertedCurrency
          }
        }
      }
    }
  }
}
'''

if __name__ == "__main__":

    clientId = os.environ["NEXAR_CLIENT_ID"]
    clientSecret = os.environ["NEXAR_CLIENT_SECRET"]
    nexar = NexarClient(clientId, clientSecret)

    queries = []
    for mpn in mpns:
        queries += [{"start": 0, "limit": 1, "mpn": mpn}]

    variables = {
        "queries": queries
    }
    results = nexar.get_query(gqlQuery, variables)

    if results:
        with open("output.csv", "w", newline="") as outputFile:
            writer = csv.writer(outputFile)

            for query in results.get("supMultiMatch"):
                writer.writerow(["MPN","Name"])
                writer.writerow([query.get("parts")[0].get("mpn"),query.get("parts")[0].get("name")])
                writer.writerow("")

                for seller in query.get("parts",{})[0].get("sellers"):
                    writer.writerow(["Seller ID","Seller Name"])
                    writer.writerow([seller.get("company").get("id"),seller.get("company").get("name")])
                    writer.writerow("")

                    for offer in seller.get("offers"):
                        writer.writerow(["Stock",offer.get("inventoryLevel")])
                        writer.writerow("")

                        writer.writerow(["Quantity","Price"])
                        for price in offer.get("prices"):
                            writer.writerow([price.get("quantity"), price.get("convertedPrice")])
                        
                        writer.writerow("")

                    writer.writerow("")
                
                writer.writerow("")