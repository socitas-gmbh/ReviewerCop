codeunit 50203 CompliantGetDifferentArityTest
{
    procedure Read(JsonObj: JsonObject): Text
    var
        Result: Text;
    begin
        // Single-argument Get is a different overload that returns a JsonToken directly;
        // not the (Key, Token) shape this rule targets.
        Result := [|JsonObj.GetText('foo')|];
        exit(Result);
    end;
}
