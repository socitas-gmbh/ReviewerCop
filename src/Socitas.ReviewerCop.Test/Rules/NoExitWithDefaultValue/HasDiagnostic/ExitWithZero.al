codeunit 50101 ExitWithZeroTest
{
    procedure GetCount(): Integer
    begin
        [|exit(0)|];
    end;
}
