table 50103 ModifyProcTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100]) { }
    }

    procedure UpdateName(NewName: Text[100])
    begin
        Name := NewName;
        Rec.[||]Modify();
    end;
}
