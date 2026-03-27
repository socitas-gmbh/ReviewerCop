table 50303 NoCaptionTable
{
    DataClassification = CustomerContent;

    fields
    {
        field(1; [||]Name; Text[100]) { }
        field(2; [||]Amount; Decimal) { }
    }
}

page 50303 NoCaptionPage
{
    SourceTable = NoCaptionTable;

    layout
    {
        area(Content)
        {
            field(Name; Rec.Name) { }
            field(Amount; Rec.Amount)
            {
                Editable = false;
            }
        }
    }
}
