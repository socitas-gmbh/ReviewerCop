codeunit 50110 StringAlterationCallTest
{
    procedure [||]TruncateName(IsShort: Boolean; FullName: Text[250]): Text[50]
    var
        Result: Text[50];
    begin
        if IsShort then
            Result := CopyStr(FullName, 1, 10)
        else
            Result := CopyStr(FullName, 1, MaxStrLen(Result));
        exit(Result);
    end;

    procedure CleanCode(HasPrefix: Boolean; RawCode: Text[50]): Text[50]
    var
        Code: Text[50];
    begin
        if HasPrefix then
            Code := DelChr(RawCode, '<', '-')
        else
            Code := RawCode;
        exit(Code);
    end;
}
