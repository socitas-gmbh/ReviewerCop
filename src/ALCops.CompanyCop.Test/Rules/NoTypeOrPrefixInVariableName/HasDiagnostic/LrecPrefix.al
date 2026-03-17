table 50000 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100]) { }
    }
}

codeunit 50400 LrecPrefixTest
{
    procedure ProcessRecords()
    var
        [|lrecMyTable|]: Record MyTable;
        [|lrecSecondTable|]: Record MyTable;
    begin
        if lrecMyTable.FindSet() then
            repeat
            until lrecMyTable.Next() = 0;
    end;
}
