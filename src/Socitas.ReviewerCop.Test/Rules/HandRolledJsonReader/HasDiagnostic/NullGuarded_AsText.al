codeunit 50102 NullGuardedAsTextTest
{
    procedure Read(JsonObj: JsonObject): Text
    var
        FieldToken: JsonToken;
        Result: Text;
    begin
        if [|JsonObj.Get('foo', FieldToken)|] then
            if not FieldToken.AsValue().IsNull() then
                Result := FieldToken.AsValue().AsText();
        exit(Result);
    end;
}
