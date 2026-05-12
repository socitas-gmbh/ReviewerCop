codeunit 50104 ExitWithZeroDateTimeTest
{
    procedure GetDateTime(): DateTime
    begin
        [|exit(0DT)|];
    end;
}
