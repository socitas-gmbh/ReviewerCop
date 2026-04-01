codeunit 50103 ExitWithTrueTest
{
    procedure IsValid(): Boolean
    begin
        [|exit(true)|];
    end;
}
