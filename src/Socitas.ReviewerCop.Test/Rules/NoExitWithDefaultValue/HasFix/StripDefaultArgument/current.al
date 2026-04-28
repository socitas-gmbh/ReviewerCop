codeunit 50200 ExitFixTest
{
    procedure IsEnabled(): Boolean
    begin
        [|exit(false)|];
    end;
}
