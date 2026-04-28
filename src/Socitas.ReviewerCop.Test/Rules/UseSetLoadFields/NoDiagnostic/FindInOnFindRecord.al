table 50024 MyFindRecord
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100]) { }
    }
}

page 50100 MyPage
{
    SourceTable = MyFindRecord;

    trigger [||]OnFindRecord(Which: Text): Boolean
    begin
        if not Rec.Find(Which) then
            exit(false);
        exit(true);
    end;
}
