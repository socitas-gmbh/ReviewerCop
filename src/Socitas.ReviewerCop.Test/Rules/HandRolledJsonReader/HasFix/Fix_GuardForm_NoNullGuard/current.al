codeunit 50306 FixGuardNoNullGuardTest
{
    procedure ReadInteger(JsonObj: JsonObject; FieldName: Text): Integer
    var
        FieldToken: JsonToken;
    begin
        if not [|JsonObj.Get(FieldName, FieldToken)|] then
            exit;
        exit(FieldToken.AsValue().AsInteger());
    end;
}
