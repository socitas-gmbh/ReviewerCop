codeunit 50307 FixGuardFormAsObjectTest
{
    procedure ReadNested(JsonObj: JsonObject; FieldName: Text): JsonObject
    var
        FieldToken: JsonToken;
    begin
        exit(JsonObj.GetObject(FieldName, true));
    end;
}
