codeunit 50106 ExitWithNonEmptyStringTest
{
    procedure GetDefaultName(): Text
    begin
        [|exit('hello')|];
    end;
}
