table 50035 MyPrecedingCommentEntity
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100]) { }
    }
}

codeunit 50313 PrecedingLineCommentTest
{
    procedure [||]SetName(var MyRec: Record MyPrecedingCommentEntity; NewName: Text[100])
    begin
        // intentionally not using Validate — caller guarantees value is already validated
        MyRec.Name := NewName;
    end;
}
