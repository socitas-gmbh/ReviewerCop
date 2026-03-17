table 50021 MyFilterRecord
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100]) { }
        field(3; Active; Boolean) { }
    }
}

codeunit 50601 FindFirstTest
{
    procedure GetFirstRecord(var MyRec: Record MyFilterRecord): Boolean
    begin
        MyRec.SetRange(Active, true);
        exit(MyRec.[|FindFirst()|]);
    end;
}
