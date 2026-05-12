codeunit 50308 FixGuardFormAssignTest
{
    procedure ReadInto(JsonObj: JsonObject; FieldName: Text; var Result: Text)
    var
        FieldToken: JsonToken;
    begin
        if not [|JsonObj.Get(FieldName, FieldToken)|] then
            exit;
        if FieldToken.AsValue().IsNull() then
            exit;
        Result := FieldToken.AsValue().AsText();
    end;
}
