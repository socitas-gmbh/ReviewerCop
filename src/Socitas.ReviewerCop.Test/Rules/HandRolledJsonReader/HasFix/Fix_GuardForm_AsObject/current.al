codeunit 50307 FixGuardFormAsObjectTest
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
