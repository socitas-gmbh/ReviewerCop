table 50030 MyLedgerEntry
{
    fields
    {
        field(1; "Entry No."; Integer) { }
        field(2; "Customer No."; Code[20]) { }
        field(3; Amount; Decimal) { }
    }
}

table 50031 MyCustomer
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100]) { }
        field(3; Balance; Decimal)
        {
            FieldClass = FlowField;
            CalcFormula = sum(MyLedgerEntry.Amount where("Customer No." = field("No.")));
        }
        field(4; "Entry Count"; Integer)
        {
            FieldClass = FlowField;
            CalcFormula = count(MyLedgerEntry where("Customer No." = field("No.")));
        }
    }
}

codeunit 50630 SetLoadFieldsWithoutCalc
{
    procedure GetBalance(CustomerNo: Code[20]): Decimal
    var
        Customer: Record MyCustomer;
    begin
        Customer.SetLoadFields(Name, [|Balance|]);
        Customer.Get(CustomerNo);
        exit(Customer.Balance);
    end;
}
