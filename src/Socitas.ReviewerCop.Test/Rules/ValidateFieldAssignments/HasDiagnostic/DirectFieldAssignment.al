table 50030 MyEntity
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100]) { }
        field(3; Description; Text[250]) { }
    }
}

codeunit 50300 ValidateTest
{
    procedure SetEntityName(var MyRec: Record MyEntity; NewName: Text[100])
    begin
        [|MyRec.Name|] := NewName;
    end;
}
