codeunit 50108 GuardFormNoNullGuardTest
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
