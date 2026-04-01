codeunit 50500 HttpClientTest
{
    procedure SendRequest()
    var
        MyClient: [|HttpClient|];
        MyRequest: [|HttpRequestMessage|];
        MyResponse: [|HttpResponseMessage|];
        MyContent: [|HttpContent|];
        MyHeaders: [|HttpHeaders|];
    begin
        // Should flag all raw HTTP type usage
    end;
}
