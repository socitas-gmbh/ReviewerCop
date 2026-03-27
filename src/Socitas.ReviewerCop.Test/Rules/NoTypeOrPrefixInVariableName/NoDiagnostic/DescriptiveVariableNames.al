table 50002 MyDocHeader
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Amount; Decimal) { }
    }
}

codeunit 50410 DescriptiveNames
{
    procedure [||]ProcessDocuments()
    var
        DocHeader: Record MyDocHeader;
        TotalAmount: Decimal;
        IsPosted: Boolean;
    begin
        TotalAmount := 0;
        IsPosted := false;

        if DocHeader.FindSet() then
            repeat
                TotalAmount += DocHeader.Amount;
            until DocHeader.Next() = 0;
    end;
}
