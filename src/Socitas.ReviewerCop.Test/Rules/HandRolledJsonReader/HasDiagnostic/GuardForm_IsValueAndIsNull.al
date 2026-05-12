codeunit 50109 GuardFormIsValueAndIsNullTest
{
    local procedure GetTextOrEmpty(Obj: JsonObject; PropertyName: Text): Text
    var
        Token: JsonToken;
    begin
        if not [|Obj.Get(PropertyName, Token)|] then
            exit;
        if not Token.IsValue() then
            exit;
        if Token.AsValue().IsNull() then
            exit;
        exit(Token.AsValue().AsText());
    end;
}
