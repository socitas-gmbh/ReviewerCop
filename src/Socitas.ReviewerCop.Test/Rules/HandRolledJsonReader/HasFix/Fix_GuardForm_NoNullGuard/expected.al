codeunit 50306 FixGuardNoNullGuardTest
{
    procedure ReadInteger(JsonObj: JsonObject; FieldName: Text): Integer
    var
        FieldToken: JsonToken;
    begin
        exit(JsonObj.GetInteger(FieldName, true));
    end;
}
