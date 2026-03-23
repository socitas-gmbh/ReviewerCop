table 50700 CanaryLoadFieldsTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Description; Text[100]) { }
        field(3; Blocked; Boolean) { }
    }
}

codeunit 50701 CanaryUseSetLoadFields
{
    procedure Process()
    var
        CanaryRec: Record CanaryLoadFieldsTable;
    begin
        if CanaryRec.[|FindFirst()|] then
            Message(CanaryRec.Description);
    end;
}
