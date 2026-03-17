table 50023 MyTempDataRecord
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100]) { }
    }
}

codeunit 50611 TempFindSetTest
{
    procedure [||]ProcessTempRecords(var TempRec: Record MyTempDataRecord temporary)
    begin
        // Temporary records do not need SetLoadFields
        if TempRec.FindSet() then
            repeat
                Message(TempRec.Name);
            until TempRec.Next() = 0;
    end;
}
