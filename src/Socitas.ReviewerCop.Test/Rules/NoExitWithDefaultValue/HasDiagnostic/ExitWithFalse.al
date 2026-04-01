codeunit 50100 ExitWithFalseTest
{
    procedure IsEnabled(): Boolean
    begin
        [|exit(false)|];
    end;
}
