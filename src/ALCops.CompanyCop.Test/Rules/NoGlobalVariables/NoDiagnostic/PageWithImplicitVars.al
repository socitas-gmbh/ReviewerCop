table 50221 MyPageTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100]) { }
    }
}

page 50220 MyBasePage
{
    SourceTable = MyPageTable;

    layout
    {
        area(Content)
        {
            field(NameField; Rec.Name) { }
        }
    }

    trigger OnAfterGetRecord()
    begin
        [||]// Rec, xRec, CurrPage are compiler-injected — must not trigger CC0002
    end;
}
