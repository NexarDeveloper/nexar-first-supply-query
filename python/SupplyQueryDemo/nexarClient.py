"""Resources for making Nexar requests."""
import os, requests, re
from typing import Callable, Dict, Iterator
from requests_toolbelt import MultipartEncoder
from nexarToken import get_token

NEXAR_URL = "https://api.nexar.com/graphql"
NEXAR_FILE_URL = "https://files.nexar.com/Upload/WorkflowAttachment"


class NexarClient:
    def __init__(self, *args) -> None:
        self.s = requests.session()
        if len(args) == 1:
            token = args[0]
        else:
            token = get_token(args[0], args[1], ['supply.domain']).get('access_token')
        self.s.headers.update({"token": token})
        self.s.keep_alive = False

    def get_query(self, query: str, variables: Dict) -> dict:
        """Return Nexar response for the query."""
        try:
            r = self.s.post(
                NEXAR_URL,
                json={"query": query, "variables": variables},
            )

        except Exception as e:
            print(e)
            raise Exception("Error while getting Nexar response")

        response = r.json()
        if ("errors" in response):
            for error in response["errors"]: print(error["message"])
            raise SystemExit

        return response["data"]

    def upload_file(self, workspaceUrl: str, path: str, container: str) -> str:
        """Return Nexar response for the file upload."""
        try:
            multipart_data = MultipartEncoder(
                fields = {
                    'file': (os.path.basename(path), open(path, 'rb'), 'text/plain'),
                    'workspaceUrl': workspaceUrl,
                    'container': container,
                }
            )

            r = self.s.post(
                NEXAR_FILE_URL,
                data = multipart_data,
                headers = {
                    'Content-Type': multipart_data.content_type,
                }
            )

        except Exception as e:
            print(e)
            raise Exception("Error while uploading file to Nexar")

        return r.text

    class Node:
        def __init__(self, client, query: str, variables: Dict, f: Callable) -> None:
            self.client = client
            self.query = query
            self.variables = variables
            self.f = f
            self.name = re.search("after[\s]*:[\s]*\$([\w]*)", query).group(1)

        def __iter__(self) -> Iterator:
            self.pageInfo = {"hasNextPage": True}
            return self
 
        def __next__(self):
            if (not self.pageInfo["hasNextPage"]): raise StopIteration

            data = self.client.get_query(self.query, self.variables)

            self.pageInfo = self.f(data)["pageInfo"]
            self.variables[self.name] = self.pageInfo["endCursor"]
            return self.f(data)["nodes"]

    def NodeIter(self, query: str, variables: dict, f: Callable) -> Iterator:
        return NexarClient.Node(self, query, variables, f)
