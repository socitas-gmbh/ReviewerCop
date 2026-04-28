// A codeunit that implements "Http Client Handler" must use HttpClient directly in its
// Send procedure — that is the whole point of the interface.  AI0004 must not fire here.

interface "Http Client Handler"
{
    procedure Send()
}

codeunit 50700 "Client Handler" implements "Http Client Handler"
{
    procedure Send()
    var
        Client: [|HttpClient|];
        Request: [|HttpRequestMessage|];
        Response: [|HttpResponseMessage|];
    begin
        Client.Send(Request, Response);
    end;
}
