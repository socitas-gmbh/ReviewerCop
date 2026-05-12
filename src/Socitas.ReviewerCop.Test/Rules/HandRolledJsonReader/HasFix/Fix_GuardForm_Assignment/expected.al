codeunit 50308 FixGuardFormAssignTest
{
    procedure ReadInto(JsonObj: JsonObject; FieldName: Text; var Result: Text)
    var
        FieldToken: JsonToken;
    begin
        Result := JsonObj.GetText(FieldName, true);
    end;
}
