table 50022 MyLoadRecord
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100]) { }
        field(3; Amount; Decimal) { }
    }
}

codeunit 50610 SetLoadFieldsUsed
{
    procedure [||]ProcessRecords()
    var
        MyRec: Record MyLoadRecord;
    begin
        MyRec.SetLoadFields(Name, "No.");
        if MyRec.FindSet() then
            repeat
                Message(MyRec.Name);
            until MyRec.Next() = 0;
    end;
}
