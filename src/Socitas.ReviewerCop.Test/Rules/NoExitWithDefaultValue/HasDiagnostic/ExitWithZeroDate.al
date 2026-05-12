codeunit 50102 ExitWithZeroDateTest
{
    procedure GetDate(): Date
    begin
        [|exit(0D)|];
    end;
}
