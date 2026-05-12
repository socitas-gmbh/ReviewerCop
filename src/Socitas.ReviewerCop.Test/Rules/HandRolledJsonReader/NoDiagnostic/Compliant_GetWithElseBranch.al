codeunit 50201 CompliantGetWithElseBranchTest
{
    procedure Read(JsonObj: JsonObject): Text
    var
        FieldToken: JsonToken;
        Result: Text;
    begin
        // Else branch means we cannot safely collapse to the shortcut.
        if [|JsonObj.Get('foo', FieldToken)|] then
            Result := FieldToken.AsValue().AsText()
        else
            Result := 'fallback';
        exit(Result);
    end;
}
