codeunit 50310 FixGuardFormIsValueAndIsNullTest
{
    local procedure GetTextOrEmpty(Obj: JsonObject; PropertyName: Text): Text
    var
        Token: JsonToken;
    begin
        exit(Obj.GetText(PropertyName, true));
    end;
}
