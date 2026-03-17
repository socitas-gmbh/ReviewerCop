table 50033 MyTempEntity
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100]) { }
    }
}

codeunit 50311 TempRecordTest
{
    procedure [||]FillTempTable(var TempRec: Record MyTempEntity temporary)
    begin
        TempRec.Name := 'Test Record';
        TempRec."No." := 'TEST001';
        TempRec.Insert();
    end;
}
