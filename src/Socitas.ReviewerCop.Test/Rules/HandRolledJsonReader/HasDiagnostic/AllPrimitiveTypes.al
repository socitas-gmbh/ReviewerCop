codeunit 50104 AllPrimitiveTypesTest
{
    procedure Read(JsonObj: JsonObject)
    var
        T: JsonToken;
        ValText: Text;
        ValBool: Boolean;
        ValInt: Integer;
        ValBigInt: BigInteger;
        ValDecimal: Decimal;
        ValByte: Byte;
        ValChar: Char;
        ValDate: Date;
        ValDateTime: DateTime;
        ValTime: Time;
        ValDuration: Duration;
    begin
        if [|JsonObj.Get('a', T)|] then
            ValText := T.AsValue().AsText();
        if [|JsonObj.Get('b', T)|] then
            ValBool := T.AsValue().AsBoolean();
        if [|JsonObj.Get('c', T)|] then
            ValInt := T.AsValue().AsInteger();
        if [|JsonObj.Get('d', T)|] then
            ValBigInt := T.AsValue().AsBigInteger();
        if [|JsonObj.Get('e', T)|] then
            ValDecimal := T.AsValue().AsDecimal();
        if [|JsonObj.Get('f', T)|] then
            ValByte := T.AsValue().AsByte();
        if [|JsonObj.Get('g', T)|] then
            ValChar := T.AsValue().AsChar();
        if [|JsonObj.Get('h', T)|] then
            ValDate := T.AsValue().AsDate();
        if [|JsonObj.Get('i', T)|] then
            ValDateTime := T.AsValue().AsDateTime();
        if [|JsonObj.Get('j', T)|] then
            ValTime := T.AsValue().AsTime();
        if [|JsonObj.Get('k', T)|] then
            ValDuration := T.AsValue().AsDuration();
    end;
}
