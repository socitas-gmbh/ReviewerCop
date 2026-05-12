codeunit 50200 CompliantDirectGetTextTest
{
    procedure Read(JsonObj: JsonObject): Text
    var
        Result: Text;
    begin
        Result := [|JsonObj.GetText('foo', true)|];
        exit(Result);
    end;
}
