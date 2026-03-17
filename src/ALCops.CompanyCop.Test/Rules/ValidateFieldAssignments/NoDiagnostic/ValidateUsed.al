table 50032 MyValidatedEntity
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100]) { }
        field(3; "Payment Terms Code"; Code[10]) { }
    }
}

codeunit 50310 ValidateUsed
{
    procedure [||]SetEntityName(var MyRec: Record MyValidatedEntity; NewName: Text[100])
    begin
        MyRec.Validate(Name, NewName);
    end;

    procedure SetPaymentTerms(var MyRec: Record MyValidatedEntity; PaymentTermsCode: Code[10])
    begin
        MyRec.Validate("Payment Terms Code", PaymentTermsCode);
    end;
}
