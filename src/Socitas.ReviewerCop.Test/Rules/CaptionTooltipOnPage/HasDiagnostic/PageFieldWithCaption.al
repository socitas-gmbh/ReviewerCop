table 50300 CaptionTable
{
    DataClassification = CustomerContent;

    fields
    {
        field(1; Name; Text[100]) { }
        field(2; Description; Text[250]) { }
    }
}

page 50300 CaptionPage
{
    SourceTable = CaptionTable;

    layout
    {
        area(Content)
        {
            field(Name; Rec.Name)
            {
                [|Caption|] = 'Full Name';
            }
            field(Description; Rec.Description)
            {
                [|Caption|] = 'Long Description';
                [|ToolTip|] = 'Specifies the long description.';
            }
        }
    }
}
