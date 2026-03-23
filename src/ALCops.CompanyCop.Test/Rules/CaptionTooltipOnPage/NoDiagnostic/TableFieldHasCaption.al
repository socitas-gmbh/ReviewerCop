table 50302 CaptionOnTableField
{
    DataClassification = CustomerContent;

    fields
    {
        field(1; Name; Text[100])
        {
            Caption = 'Full Name';
            ToolTip = 'Specifies the full name.';
        }
    }
}

page 50302 CaptionOnTableFieldPage
{
    SourceTable = CaptionOnTableField;

    layout
    {
        area(Content)
        {
            field(Name; Rec.[||]Name) { }
        }
    }
}
