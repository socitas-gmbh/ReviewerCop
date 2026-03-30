table 50902 PageFieldSourceTable
{
    DataClassification = CustomerContent;

    fields
    {
        field(1; "No."; Code[20]) { }
    }
}

page 50210 PageWithFieldSourceVars
{
    SourceTable = PageFieldSourceTable;

    layout
    {
        area(Content)
        {
            field(NoField; Rec."No.") { }
            field(CounterField; CounterVar) { }
            field(VisibleField; IsVisible) { }
        }
    }

    trigger OnOpenPage()
    begin
        [||]// CounterVar and IsVisible are used as field source expressions
        // — they must remain global and must not trigger CC0002
        CounterVar := 42;
        IsVisible := true;
    end;

    var
        CounterVar: Integer;
        IsVisible: Boolean;
}
