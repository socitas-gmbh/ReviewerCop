// Reproduces the user-reported false positive:
//   RestClient.Initialize(ClientHandler) where
//     - Initialize takes a single Interface "Http Client Handler" parameter
//     - The procedure receives Rest Client as a var parameter
//     - ClientHandler is a local codeunit defined in this app implementing the interface
//
// Expected behaviour: no AI0005 diagnostic.

interface "Http Client Handler"
{
    procedure Send()
}

codeunit 2354 "Rest Client"
{
    procedure Initialize(NewHttpClientHandler: Interface "Http Client Handler")
    begin
    end;

    procedure SetBaseAddress(BaseAddress: Text)
    begin
    end;
}

codeunit 50700 "Client Handler" implements "Http Client Handler"
{
    procedure Send()
    begin
    end;
}

codeunit 50601 SingleIfaceVarParamTest
{
    local procedure InitRestClient(var RestClient: Codeunit "Rest Client")
    var
        ClientHandler: Codeunit "Client Handler";
    begin
        [||]RestClient.Initialize(ClientHandler);
        RestClient.SetBaseAddress('https://example.com');
    end;
}
