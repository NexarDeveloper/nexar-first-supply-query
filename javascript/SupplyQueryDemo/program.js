const nx = require('../NexarClient/nexarClient')
const clientId = process.env.NEXAR_CLIENT_ID ??
    (() => {throw new Error("Please set environment variable 'NEXAR_CLIENT_ID'")})()
const clientSecret = process.env.NEXAR_CLIENT_SECRET ??
    (() => {throw new Error("Please set environment variable 'NEXAR_CLIENT_SECRET'")})()
const nexar = new nx.NexarClient(clientId, clientSecret)

const gqlQuery = `query Search($mpn: String!) {
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
  }`

  const readline = require('readline');
  const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout,
    prompt: 'Search MPN: '
  });
  
  rl.on('line', async (MPN) => {
    if (!MPN.length) {
        rl.close()
        return
    }

    // run the query
    const response = await nexar.query(gqlQuery, {'mpn': MPN})
    const results = response?.data?.supSearchMpn?.results

    // check if no results
    if (!results || results.length == 0) {
        console.log('Sorry, no parts found')
        console.log()
        return
    }

    // print the results
    for (const it of results) {
        console.log(`MPN: ${it?.part?.mpn}`)
        console.log(`Desciption: ${it?.part?.shortDescription}`)
        console.log(`Manufacturer: ${it?.part?.manufacturer?.name}`)
        console.log();
    }    
    rl.prompt()
})
rl.prompt()