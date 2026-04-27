interface "Http Client Handler"
{
    procedure Send()
}

interface "Http Authentication"
{
    procedure Authenticate()
}

codeunit 2354 "Rest Client"
{
    procedure Initialize(Handler: Interface "Http Client Handler"; Auth: Interface "Http Authentication")
    begin
    end;
}

codeunit 50700 "Client Handler" implements "Http Client Handler"
{
    procedure Send()
    begin
    end;
}

codeunit 50701 "Http Authentication Basic" implements "Http Authentication"
{
    procedure Authenticate()
    begin
    end;
}

codeunit 50601 WithLocalHandlerAndAuthTest
{
    procedure [||]InitRestClient()
    var
        RestClient: Codeunit "Rest Client";
        SocitasHttpClientHandler: Codeunit "Client Handler";
        HttpBasicAuth: Codeunit "Http Authentication Basic";
    begin
        RestClient.Initialize(SocitasHttpClientHandler, HttpBasicAuth);
    end;
}
