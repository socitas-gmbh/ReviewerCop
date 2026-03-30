table 50900 ReportUnusedVarTable
{
    DataClassification = CustomerContent;

    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100]) { }
    }
}

report 50003 ReportWithUnusedGlobalVar
{
    dataset
    {
        dataitem(MyItem; ReportUnusedVarTable)
        {
            column(NameCol; Name) { }
        }
    }

    procedure ComputeExtra()
    begin
        // Helper var is only used in a procedure — it can be local
    end;

    var
        [|HelperVar|]: Text;
}
