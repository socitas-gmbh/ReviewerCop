table 50301 CaptionExtTable
{
    DataClassification = CustomerContent;

    fields
    {
        field(1; Name; Text[100]) { }
    }
}

page 50301 CaptionExtBasePage
{
    SourceTable = CaptionExtTable;

    layout
    {
        area(Content)
        {
            field(Name; Rec.Name) { }
        }
    }
}

pageextension 50301 CaptionExtPageExt extends CaptionExtBasePage
{
    layout
    {
        modify(Name)
        {
            [|Caption|] = 'Full Name';
            [|ToolTip|] = 'Specifies the full name.';
        }
    }
}
