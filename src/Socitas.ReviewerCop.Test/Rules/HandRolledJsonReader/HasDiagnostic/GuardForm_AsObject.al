codeunit 50109 GuardFormAsObjectTest
{
    procedure ReadNested(JsonObj: JsonObject; FieldName: Text): JsonObject
    var
        FieldToken: JsonToken;
    begin
        if not [|JsonObj.Get(FieldName, FieldToken)|] then
            exit;
        exit(FieldToken.AsObject());
    end;
}
