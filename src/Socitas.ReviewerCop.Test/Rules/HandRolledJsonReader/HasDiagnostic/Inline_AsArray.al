codeunit 50106 InlineAsArrayTest
{
    procedure Read(JsonObj: JsonObject): JsonArray
    var
        FieldToken: JsonToken;
        Items: JsonArray;
    begin
        if [|JsonObj.Get('items', FieldToken)|] then
            Items := FieldToken.AsArray();
        exit(Items);
    end;
}
