table 50906 PropValueTable
{
    DataClassification = CustomerContent;

    fields
    {
        field(1; "No."; Code[20]) { }
    }
}

page 50216 PageWithPropertyValueVars
{
    SourceTable = PropValueTable;

    layout
    {
        area(Content)
        {
            field(NoField; Rec."No.") { }
        }
    }

    actions
    {
        area(processing)
        {
            action(MyAction)
            {
                Enabled = SendCustomEnabled;

                trigger OnAction()
                begin
                    [||]Message('Hello');
                end;
            }
        }
    }

    trigger OnAfterGetRecord()
    begin
        SendCustomEnabled := true;
    end;

    var
        SendCustomEnabled: Boolean;
}
