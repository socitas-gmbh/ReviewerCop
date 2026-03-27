table 50220 MyExtendedTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100]) { }
    }
}

tableextension 50220 "MyExtendedTable Ext" extends MyExtendedTable
{
    fields
    {
        field(51000; MyExtraField; Boolean) { }
    }

    trigger OnBeforeInsert()
    begin
        [||]// Rec, xRec, CurrFieldNo are compiler-injected — must not trigger CC0002
    end;
}
