codeunit 50105 InlineAsObjectTest
{
    procedure Read(JsonObj: JsonObject): JsonObject
    var
        FieldToken: JsonToken;
        Nested: JsonObject;
    begin
        if [|JsonObj.Get('payload', FieldToken)|] then
            Nested := FieldToken.AsObject();
        exit(Nested);
    end;
}
