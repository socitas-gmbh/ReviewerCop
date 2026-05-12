codeunit 50110 GuardFormIsObjectGuardTest
{
    local procedure GetChild(Parent: JsonObject; PropertyName: Text): JsonObject
    var
        Token: JsonToken;
    begin
        if not [|Parent.Get(PropertyName, Token)|] then
            exit;
        if not Token.IsObject() then
            exit;
        exit(Token.AsObject());
    end;
}
