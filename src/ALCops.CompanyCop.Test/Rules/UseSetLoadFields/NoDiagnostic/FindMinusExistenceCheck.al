table 50024 MyExistenceRecord
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100]) { }
    }
}

codeunit 50612 FindMinusExistenceCheckTest
{
    procedure [||]HasMoreThanOneRecord()
    var
        MyRec: Record MyExistenceRecord;
    begin
        // Established pattern: check if a table has more than one record
        // Find('-') + Next() is an existence check — SetLoadFields is not needed
        if MyRec.Find('-') and (MyRec.Next() <> 0) then
            Message('More than one record exists');
    end;
}
