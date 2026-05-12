codeunit 50204 CompliantNonTokenTest
{
    procedure Read(JsonObj: JsonObject; SeparateToken: JsonToken): Text
    var
        FieldToken: JsonToken;
        Result: Text;
    begin
        // The if-body reads from SeparateToken, not from the matched FieldToken,
        // so the pattern does not apply.
        if [|JsonObj.Get('foo', FieldToken)|] then
            Result := SeparateToken.AsValue().AsText();
        exit(Result);
    end;
}
