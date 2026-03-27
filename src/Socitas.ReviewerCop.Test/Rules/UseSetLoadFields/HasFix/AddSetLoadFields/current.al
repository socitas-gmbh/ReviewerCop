table 50026 MyFixRecord
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100]) { }
        field(3; City; Text[100]) { }
    }
}

codeunit 50614 AddSetLoadFieldsFixTest
{
    procedure ProcessRecords()
    var
        MyRec: Record MyFixRecord;
    begin
        if MyRec.[|FindSet()|] then
            repeat
                Message('%1 - %2', MyRec.Name, MyRec.City);
        until MyRec.Next() = 0;
    end;
}
