table 50001 MySetupTable
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        field(2; Description; Text[100]) { }
    }
}

codeunit 50401 GrecPrefixTest
{
    var
        [|grecSetupRecord|]: Record MySetupTable;

    procedure Initialize()
    begin
        grecSetupRecord.Get('');
    end;
}
