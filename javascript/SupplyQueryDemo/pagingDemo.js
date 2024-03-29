const nx = require("../NexarClient/nexarClient");
const clientId =
  process.env.NEXAR_CLIENT_ID ??
  (() => {
    throw new Error("Please set environment variable 'NEXAR_CLIENT_ID'");
  })();
const clientSecret =
  process.env.NEXAR_CLIENT_SECRET ??
  (() => {
    throw new Error("Please set environment variable 'NEXAR_CLIENT_SECRET'");
  })();
const nexar = new nx.NexarClient(clientId, clientSecret);

const gqlQuery = `query Search($search: String!, $start: Int) {
    supSearch(q: $search, limit: 100, start: $start) {
        hits
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
}`;

const readline = require("readline");
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout,
  prompt: "Search: ",
});

rl.on("line", async (search) => {
  if (!search.length) {
    rl.close();
    return;
  }

  async function runQuery() {
    let queryResults = [];
    let cursor = 0;
    let hasMoreResults = true;

    while (hasMoreResults) {
      await nexar
        .query(gqlQuery, { search: search, start: cursor })
        .then(function (response) {
          cursor += response.data.supSearch.results.length;
          for (result of response.data.supSearch.results) {
            queryResults.push(result);
          }
          hasMoreResults =
            response.data.supSearch.hits > cursor && 1000 > cursor;
        })
        .catch((err) => console.log(err));
    }

    return queryResults;
  }

  runQuery()
    .then(function (results) {
      for (const it of results) {
        console.log(`MPN: ${it?.part?.mpn}`);
        console.log(`Description: ${it?.part?.shortDescription}`);
        console.log(`Manufacturer: ${it?.part?.manufacturer?.name}`);
        console.log();
      }

      rl.prompt();
    })
    .catch((err) => console.log(err));
});

rl.prompt();
