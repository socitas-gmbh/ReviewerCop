codeunit 2354 "Rest Client"
{
    procedure Initialize()
    begin
    end;
}

codeunit 50600 NoLocalHandlerTest
{
    procedure SendRequest()
    var
        RestClient: Codeunit "Rest Client";
    begin
        [|RestClient.Initialize()|];
    end;
}
