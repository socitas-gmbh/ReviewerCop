codeunit 50211 MyCodeunitLocalVars
{
    procedure [||]ComputeTotal(UnitPrice: Decimal; Quantity: Integer): Decimal
    var
        Total: Decimal;
        TaxRate: Decimal;
    begin
        TaxRate := 0.19;
        Total := UnitPrice * Quantity * (1 + TaxRate);
        exit(Total);
    end;
}
