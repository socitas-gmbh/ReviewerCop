codeunit 50103 ExitWithZeroTimeTest
{
    procedure GetTime(): Time
    begin
        [|exit(0T)|];
    end;
}
