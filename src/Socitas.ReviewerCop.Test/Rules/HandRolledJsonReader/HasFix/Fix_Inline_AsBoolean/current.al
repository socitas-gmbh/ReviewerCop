codeunit 50302 FixInlineAsBooleanTest
{
    procedure Read(JsonObj: JsonObject): Boolean
    var
        FieldToken: JsonToken;
        Result: Boolean;
    begin
        if [|JsonObj.Get('enabled', FieldToken)|] then
            Result := FieldToken.AsValue().AsBoolean();
        exit(Result);
    end;
}
