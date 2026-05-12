codeunit 50206 CompliantBeginEndBlockTest
{
    procedure Read(JsonObj: JsonObject): Text
    var
        FieldToken: JsonToken;
        Result: Text;
    begin
        // Multi-statement body — collapsing would lose the side-effect statements.
        if [|JsonObj.Get('foo', FieldToken)|] then begin
            Result := FieldToken.AsValue().AsText();
            Message('read field foo');
        end;
        exit(Result);
    end;
}
