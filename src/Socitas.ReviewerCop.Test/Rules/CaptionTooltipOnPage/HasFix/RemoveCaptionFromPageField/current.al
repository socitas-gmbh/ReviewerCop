table 50304 RemoveCaptionTable
{
    DataClassification = CustomerContent;

    fields
    {
        field(1; Name; Text[100]) { }
    }
}

page 50304 RemoveCaptionPage
{
    SourceTable = RemoveCaptionTable;

    layout
    {
        area(Content)
        {
            field(Name; Rec.Name)
            {
                [|Caption|] = 'Full Name';
            }
        }
    }
}
