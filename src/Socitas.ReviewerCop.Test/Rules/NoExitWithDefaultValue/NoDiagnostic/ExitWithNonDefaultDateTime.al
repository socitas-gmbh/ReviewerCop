codeunit 50106 ExitWithNonDefaultDateTimeTest
{
    procedure GetNow(): DateTime
    begin
        [|exit(CurrentDateTime)|];
    end;

    procedure GetToday(): Date
    begin
        [|exit(Today)|];
    end;
}
