table 50903 PageUnusedVarTable
{
    DataClassification = CustomerContent;

    fields
    {
        field(1; "No."; Code[20]) { }
    }
}

page 50211 PageWithUnusedGlobalVar
{
    SourceTable = PageUnusedVarTable;

    layout
    {
        area(Content)
        {
            field(NoField; Rec."No.") { }
        }
    }

    trigger OnOpenPage()
    begin
        // HelperVar is not used as a field source expression — can be local
    end;

    var
        [|HelperVar|]: Text;
}
