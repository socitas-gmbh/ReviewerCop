table 50034 MyInlineCommentEntity
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100]) { }
    }
}

codeunit 50312 InlineCommentTest
{
    procedure [||]SetName(var MyRec: Record MyInlineCommentEntity; NewName: Text[100])
    begin
        MyRec.Name := NewName; // intentionally not using Validate — no business logic on this field
    end;
}
