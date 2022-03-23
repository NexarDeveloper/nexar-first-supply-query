'''Example request for extracting GraphQL part data.'''
import os, sys
from nexarClient import NexarClient

QUERY_MPN = '''
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
  }
'''

if __name__ == '__main__':

    clientId = os.environ['NEXAR_CLIENT_ID']
    clientSecret = os.environ['NEXAR_CLIENT_SECRET']
    nexar = NexarClient(clientId, clientSecret)

    while (True):
        mpn = input('Search MPN: ')

        if not mpn:
            sys.exit()

        variables = {
            'mpn': mpn
        }
        results = nexar.get_query(QUERY_MPN, variables)

        if results:
            for it in results.get("supSearchMpn",{}).get("results",{}):
                print(f'MPN: {it.get("part",{}).get("mpn")}')
                print(f'Description: {it.get("part",{}).get("shortDescription")}')
                print(f'Manufacturer: {it.get("part",{}).get("manufacturer",{}).get("name")}')
                print()
        else:
            print('Sorry, no parts found')
            print()