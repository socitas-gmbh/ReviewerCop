codeunit 50208 CompliantGuardNoTermTest
{
    procedure Validate(JsonObj: JsonObject): Boolean
    var
        FieldToken: JsonToken;
    begin
        // 'if not Get then exit' is a guard, but the procedure never extracts a value;
        // there is no As<Type> chain to collapse.
        if not [|JsonObj.Get('foo', FieldToken)|] then
            exit(false);
        exit(true);
    end;
}
