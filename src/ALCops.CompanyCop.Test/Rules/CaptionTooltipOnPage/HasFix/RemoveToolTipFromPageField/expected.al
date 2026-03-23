table 50305 RemoveToolTipTable
{
    DataClassification = CustomerContent;

    fields
    {
        field(1; Description; Text[250]) { }
    }
}

page 50305 RemoveToolTipPage
{
    SourceTable = RemoveToolTipTable;

    layout
    {
        area(Content)
        {
            field(Description; Rec.Description)
            {
            }
        }
    }
}
