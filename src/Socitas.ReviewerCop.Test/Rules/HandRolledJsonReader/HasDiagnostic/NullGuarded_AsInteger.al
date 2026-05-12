codeunit 50103 NullGuardedAsIntegerTest
{
    procedure Read(JsonObj: JsonObject): Integer
    var
        FieldToken: JsonToken;
        Result: Integer;
    begin
        if [|JsonObj.Get('count', FieldToken)|] then
            if not FieldToken.AsValue().IsNull() then
                Result := FieldToken.AsValue().AsInteger();
        exit(Result);
    end;
}
