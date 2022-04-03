const nx = require('../NexarClient/nexarClient')
const clientId = process.env.NEXAR_CLIENT_ID ??
    (() => {throw new Error("Please set environment variable 'NEXAR_CLIENT_ID'")})()
const clientSecret = process.env.NEXAR_CLIENT_SECRET ??
    (() => {throw new Error("Please set environment variable 'NEXAR_CLIENT_SECRET'")})()
const nexar = new nx.NexarClient(clientId, clientSecret, nx.NexarClient.scopes.design)

const gqlQuery = `query Workspaces {
    desWorkspaces {
      url
      name
      description
      location {
        apiServiceUrl
      }
    }
  }`

let workspaces = nexar.query(gqlQuery)
    .then(response => response.data.desWorkspaces)

const gqlQuery2 = `query Projects($url: String!, $end: String) {
    desProjects(workspaceUrl: $url, first: 2, after: $end) {
      nodes {
        id
        name
        description
      }
      pageInfo {
        hasNextPage
        endCursor
      }
    }
  }`

workspaces
    .then(async workspaces => {
        console.log(`projects for workspace: ${workspaces[0].name}`)
        nexar.host = workspaces[0].location.apiServiceUrl

        let gqlVariables = {'url': workspaces[0].url}
        let projects = nexar.pageGen(gqlQuery2, gqlVariables, 'end', (data) => data.desProjects)

        for await (const page of projects) {
            for (const project of page) {
                console.log(`Project Id: ${project?.id}`)
                console.log(`Name: ${project?.name}`)
                console.log(`Description: ${project?.description}`)
                console.log()
            }
        }
    })