codeunit 50302 FixInlineAsBooleanTest
{
    procedure Read(JsonObj: JsonObject): Boolean
    var
        FieldToken: JsonToken;
        Result: Boolean;
    begin
        Result := JsonObj.GetBoolean('enabled', true);
        exit(Result);
    end;
}
