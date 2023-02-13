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

  var pages = {};
  var cursor = 0;

  function getPage(hasNextPage) {
    if (hasNextPage) {
      nexar
        .query(gqlQuery, { search: search, start: cursor })
        .then(function (response) {
          cursor += response.data.supSearch.results.length;
          pages[cursor] = response.data.supSearch.results;
          getPage(response.data.supSearch.hits > cursor && 1000 > cursor);
        })
        .catch((err) => console.log(err));
    } else {
      const data = [].concat.apply([], Object.values(pages));
      for (const it of data) {
        console.log(`MPN: ${it?.part?.mpn}`);
        console.log(`Desciption: ${it?.part?.shortDescription}`);
        console.log(`Manufacturer: ${it?.part?.manufacturer?.name}`);
        console.log();
      }
      rl.prompt();
    }
  }
  getPage(true);
});
rl.prompt();
