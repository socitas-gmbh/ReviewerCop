codeunit 50301 FixNullGuardedAsTextTest
{
    procedure Read(JsonObj: JsonObject): Text
    var
        FieldToken: JsonToken;
        Result: Text;
    begin
        Result := JsonObj.GetText('foo', true);
        exit(Result);
    end;
}
