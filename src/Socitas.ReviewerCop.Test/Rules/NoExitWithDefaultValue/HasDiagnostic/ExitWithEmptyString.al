codeunit 50102 ExitWithEmptyStringTest
{
    procedure GetName(): Text
    begin
        [|exit('')|];
    end;
}
