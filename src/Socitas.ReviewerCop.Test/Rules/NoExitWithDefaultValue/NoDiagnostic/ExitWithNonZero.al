codeunit 50104 ExitWithNonZeroTest
{
    procedure GetDefaultCount(): Integer
    begin
        [|exit(42)|];
    end;
}
