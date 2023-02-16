# nexar-first-supply-query
[nexar.com]: https://nexar.com/
Simple console app which looks up supplier parts by MPN

## Prerequisites

You need an application at [nexar.com] with the Supply scope.
Use the application client ID and secret and set environment variables `NEXAR_CLIENT_ID` and `NEXAR_CLIENT_SECRET`.

## Understanding these Examples

Each of these examples are using a "nexarClient" or "supplyClient" class. This class provides the necessary functionality to retrieve tokens and then use them to query the API. Then there are example runnable files that use this client to perform a given query and then display the data.
