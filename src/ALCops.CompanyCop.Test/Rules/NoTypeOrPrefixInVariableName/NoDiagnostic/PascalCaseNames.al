codeunit 50411 PascalCaseNames
{
    procedure [||]Calculate(BaseAmount: Decimal; TaxRate: Decimal): Decimal
    var
        TaxAmount: Decimal;
        TotalAmount: Decimal;
        MaxRetries: Integer;
    begin
        MaxRetries := 3;
        TaxAmount := BaseAmount * TaxRate;
        TotalAmount := BaseAmount + TaxAmount;
        exit(TotalAmount);
    end;
}
