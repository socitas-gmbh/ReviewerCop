codeunit 50303 FixInlineAsObjectTest
{
    procedure Read(JsonObj: JsonObject): JsonObject
    var
        FieldToken: JsonToken;
        Nested: JsonObject;
    begin
        Nested := JsonObj.GetObject('payload', true);
        exit(Nested);
    end;
}
