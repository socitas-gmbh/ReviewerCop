codeunit 50305 FixGuardFormAsTextTest
{
    procedure ReadOptionalText(JsonObj: JsonObject; FieldName: Text): Text
    var
        FieldToken: JsonToken;
    begin
        if not [|JsonObj.Get(FieldName, FieldToken)|] then
            exit;
        if FieldToken.AsValue().IsNull() then
            exit;
        exit(FieldToken.AsValue().AsText());
    end;
}
