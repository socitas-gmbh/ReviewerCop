codeunit 50205 CompliantDiffTerminalTest
{
    procedure Read(JsonObj: JsonObject): Text
    var
        FieldToken: JsonToken;
        Result: Text;
    begin
        // Body invokes WriteTo (serialization) rather than an As<Type> accessor —
        // no JsonObject shortcut covers this case.
        if [|JsonObj.Get('foo', FieldToken)|] then
            FieldToken.WriteTo(Result);
        exit(Result);
    end;
}
