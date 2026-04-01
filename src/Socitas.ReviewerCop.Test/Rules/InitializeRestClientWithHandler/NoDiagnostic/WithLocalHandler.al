codeunit 2354 "Rest Client"
{
    procedure Initialize(Handler: Codeunit "Client Handler")
    begin
    end;
}

codeunit 50700 "Client Handler"
{
    procedure Send()
    begin
    end;
}

codeunit 50601 WithLocalHandlerTest
{
    procedure [||]SendRequest()
    var
        RestClient: Codeunit "Rest Client";
        MyHandler: Codeunit "Client Handler";
    begin
        RestClient.Initialize(MyHandler);
    end;
}
