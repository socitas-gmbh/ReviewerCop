table 50203 DataClassFieldDiffersFromTable
{
    DataClassification = CustomerContent;

    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100])
        {
            [||]DataClassification = SystemMetadata;
        }
    }
}
