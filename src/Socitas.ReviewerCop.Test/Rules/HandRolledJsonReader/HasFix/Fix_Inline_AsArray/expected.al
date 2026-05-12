codeunit 50304 FixInlineAsArrayTest
{
    procedure Read(JsonObj: JsonObject): JsonArray
    var
        FieldToken: JsonToken;
        Items: JsonArray;
    begin
        Items := JsonObj.GetArray('items', true);
        exit(Items);
    end;
}
