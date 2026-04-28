table 50100 MyTable
{
    fields
    {
        field(1; Name; Text[100]) { }
    }
}

tableextension 50100 MyTableExt extends MyTable
{
    procedure OpenServiceCodesSOC()
    begin
    end;
}
