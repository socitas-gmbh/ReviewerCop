codeunit 50202 CompliantGetExistsTest
{
    procedure HasField(JsonObj: JsonObject): Boolean
    var
        FieldToken: JsonToken;
    begin
        // Get used only as an existence probe; body does not extract the value.
        if [|JsonObj.Get('foo', FieldToken)|] then
            exit(true);
        exit(false);
    end;
}
