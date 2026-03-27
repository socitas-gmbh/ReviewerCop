table 50720 CanaryDataClassTable
{
    DataClassification = CustomerContent;

    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100])
        {
            [|DataClassification|] = CustomerContent;
        }
    }
}
