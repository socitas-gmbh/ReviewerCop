table 50102 ModifyOnInsertTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100]) { }
    }

    trigger OnInsert()
    begin
        Rec.[||]Modify();
    end;
}
