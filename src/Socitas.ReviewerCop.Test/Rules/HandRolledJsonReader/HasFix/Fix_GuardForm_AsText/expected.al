codeunit 50305 FixGuardFormAsTextTest
{
    procedure ReadOptionalText(JsonObj: JsonObject; FieldName: Text): Text
    var
        FieldToken: JsonToken;
    begin
        exit(JsonObj.GetText(FieldName, true));
    end;
}
