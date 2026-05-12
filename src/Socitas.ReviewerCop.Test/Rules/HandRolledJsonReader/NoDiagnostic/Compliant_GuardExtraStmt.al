codeunit 50207 CompliantGuardExtraStmtTest
{
    procedure Read(JsonObj: JsonObject): Text
    var
        FieldToken: JsonToken;
    begin
        // A statement between the anchor and the terminal disqualifies the pattern:
        // the procedure does more than fetch + return.
        if not [|JsonObj.Get('foo', FieldToken)|] then
            exit;
        Message('inspecting payload');
        exit(FieldToken.AsValue().AsText());
    end;
}
