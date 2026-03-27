table 50027 MyUpdateRecord
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100]) { }
        field(3; City; Text[100]) { }
    }
}

codeunit 50615 UpdateSetLoadFieldsFixTest
{
    procedure ProcessRecords()
    var
        MyRec: Record MyUpdateRecord;
    begin
        MyRec.SetLoadFields(Name);
        if MyRec.[|FindSet()|] then
            repeat
                Message('%1 - %2', MyRec.Name, MyRec.City);
        until MyRec.Next() = 0;
    end;
}
