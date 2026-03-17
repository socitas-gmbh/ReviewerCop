table 50100 ModifyOnValidateTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100])
        {
            trigger OnValidate()
            begin
            end;
        }
    }
}
