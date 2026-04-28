// Scenario: local handler + an auth codeunit that is NOT defined in this file.
// This exercises the path where arg 0 (handler) is a local codeunit but arg 1 (auth)
// cannot be resolved as local. AI0005 must not fire because the handler IS local.
//
// Note: in the test compilation every symbol is "in source", so "Http Authentication Basic"
// is defined here to avoid a compilation error.  In real projects it would come from the
// system module and IsDefinedInSource() would return false for it, triggering the
// continue-past-non-local-codeunit path.  The handler is always processed first, so the
// rule correctly returns early without a diagnostic.

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

codeunit 50601 WithLocalHandlerSystemAuthTest
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
