VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "Sheet1"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Sub runSupplyExample()

    ' Declare necessary objects and variables
    Dim httpRequest As Object
    Dim url As String
    Dim query As String
    Dim requestBody As String
    Dim apiKey As String
    Dim responseText As String
    Dim filePath As String
    Dim fileContent As String
    Dim jsonResponse As Object
    Dim reportNode As Object
    Dim i As Integer
    Dim mpn As String

    userInput = InputBox("Please enter MPN: ", "MPN")
    
    ' CHANGE FILEPATH TO YOUR FILEPATH OF YOUR API KEY
    filePath = "filePath\apikey.txt"
    
    ' Open the text file and read the API key
    apiKey = ReadApiKeyFromFile(filePath)
    
    ' Set up the URL (replace with your API endpoint)
    url = "https://api.nexar.com/graphql"
    
    ' Set up the GraphQL query as a JSON string
    query = " { ""query"": ""query SupplySearch { supSearchMpn(q: \""" & userInput & "\"", limit: 2) { results { part { mpn shortDescription manufacturer { name } specs { attribute { shortname } value } } } } }"" }"

    ' Create an HTTP request object
    Set httpRequest = CreateObject("MSXML2.ServerXMLHTTP.6.0")
    
    ' Initialize and send the HTTP POST request
    With httpRequest
        .Open "POST", url, False  ' Set the HTTP method to POST and the URL
        .setRequestHeader "Content-Type", "application/json"  ' GraphQL requires JSON payload
        
        .setRequestHeader "Authorization", "Bearer " & apiKey

        ' Send the request with the query in the body
        .Send query
    End With
    
    ' Get the response text
    responseText = httpRequest.responseText
    
    ' Parse the JSON response using the JSON converter
    Set jsonResponse = JsonConverter.ParseJson(responseText)
    
    ' Extract the nodes from the JSON response
    Dim results As Object
    Set results = jsonResponse("data")("supSearchMpn")("results")
    
    ' Start writing data to Excel (starting from cell A1)
    i = 1  ' Start at row 1
    Dim ws As Worksheet
    Set ws = ThisWorkbook.Sheets("Sheet1") ' Use the desired sheet
    
    ' Write headers
    ws.Cells(1, 1).Value = "MPN"
    ws.Cells(1, 2).Value = "Short Description"
    ws.Cells(1, 3).Value = "Manufacturer"
    ws.Cells(1, 4).Value = "Attribute"
    ws.Cells(1, 5).Value = "Value"
    

    ' Loop through each result and write to cells
    For Each result In results
        If IsObject(result) Then
            i = i + 1
            ws.Cells(i, 1).Value = result("part")("mpn")
            ws.Cells(i, 2).Value = result("part")("shortDescription")
            ws.Cells(i, 3).Value = result("part")("manufacturer")("name")
            
            ' Loop through specs
            Dim specRow As Integer
            specRow = i
            
            For Each spec In result("part")("specs")
                If IsObject(spec) Then
                    ws.Cells(specRow, 4).Value = spec("attribute")("shortname")
                    ws.Cells(specRow, 5).Value = spec("value")
                    specRow = specRow + 1 ' Move to the next row for the next spec
                End If
            Next spec
        End If
    Next result
    
    MsgBox "Data written to cells successfully!"
    
    ' Clean up
    Set httpRequest = Nothing
    Set jsonResponse = Nothing
    Set results = Nothing
    
End Sub

Function ReadApiKeyFromFile(filePath As String) As String
    Dim fileNum As Integer
    Dim apiKey As String
    fileNum = FreeFile ' Get the next available file number

    ' Open the file for input (reading)
    Open filePath For Input As fileNum
    
    ' Read the API key (assuming it's on the first line)
    Line Input #fileNum, apiKey
    
    ' Close the file
    Close fileNum
    
    ' Return the API key
    ReadApiKeyFromFile = apiKey
End Function
    
    
    
