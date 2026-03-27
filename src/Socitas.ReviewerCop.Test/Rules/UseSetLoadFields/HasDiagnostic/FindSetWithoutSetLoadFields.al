table 50020 MyDataRecord
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100]) { }
        field(3; Amount; Decimal) { }
    }
}

codeunit 50600 SetLoadFieldsTest
{
    procedure ProcessRecords()
    var
        MyRec: Record MyDataRecord;
    begin
        if MyRec.[|FindSet()|] then
            repeat
                // Only accessing Name, but all fields are loaded
                Message(MyRec.Name);
            until MyRec.Next() = 0;
    end;
}
