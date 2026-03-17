table 50025 MyPassThroughRecord
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100]) { }
    }
}

codeunit 50613 PassToFunctionTest
{
    procedure [||]ProcessRecords()
    var
        MyRec: Record MyPassThroughRecord;
    begin
        PrepareRecord(MyRec);
        if MyRec.FindSet() then
            repeat
                Message(MyRec.Name);
            until MyRec.Next() = 0;
    end;

    local procedure PrepareRecord(var AnyRec: Record MyPassThroughRecord)
    begin
        // Intentionally empty.
    end;
}
