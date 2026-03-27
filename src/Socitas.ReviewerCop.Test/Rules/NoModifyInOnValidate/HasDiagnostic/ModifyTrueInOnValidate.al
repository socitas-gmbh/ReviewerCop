table 50101 ModifyTrueOnValidateTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100])
        {
            trigger OnValidate()
            begin
                [|Rec.Modify(true)|];
            end;
        }
    }
}
