report 50002 ReportWithImplicitVars
{
    trigger OnPreReport()
    begin
        [||]// CurrReport is compiler-injected — must not trigger CC0002
        CurrReport.Quit();
    end;
}
