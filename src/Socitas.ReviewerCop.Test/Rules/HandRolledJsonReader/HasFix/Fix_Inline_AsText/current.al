codeunit 50300 FixInlineAsTextTest
{
    procedure Read(JsonObj: JsonObject): Text
    var
        FieldToken: JsonToken;
        Result: Text;
    begin
        if [|JsonObj.Get('foo', FieldToken)|] then
            Result := FieldToken.AsValue().AsText();
        exit(Result);
    end;
}
