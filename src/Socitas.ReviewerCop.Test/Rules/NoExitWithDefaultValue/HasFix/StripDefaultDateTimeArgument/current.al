codeunit 50201 ExitDateTimeFixTest
{
    procedure GetDateTime(): DateTime
    begin
        [|exit(0DT)|];
    end;
}
