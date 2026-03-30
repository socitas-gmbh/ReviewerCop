table 50901 ReportColSourceTable
{
    DataClassification = CustomerContent;

    fields
    {
        field(1; "No."; Code[20]) { }
    }
}

report 50001 ReportWithColumnSourceVars
{
    dataset
    {
        dataitem(MyItem; ReportColSourceTable)
        {
            column(NameCol; NameVar) { }
            column(CodeCol; CodeVar) { }
            column(ComplexCol; Format(FormattedVar)) { }
        }
    }

    trigger OnPreReport()
    begin
        [||]// NameVar, CodeVar, and FormattedVar are used as column source expressions
        // — they must remain global and must not trigger CC0002
    end;

    var
        NameVar: Text;
        CodeVar: Code[20];
        FormattedVar: Decimal;
}
