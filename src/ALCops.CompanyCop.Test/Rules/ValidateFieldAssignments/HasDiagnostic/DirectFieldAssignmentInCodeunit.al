table 50031 MyDocumentEntity
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; "Payment Terms Code"; Code[10]) { }
        field(3; "Due Date"; Date) { }
    }
}

codeunit 50301 DocumentValidation
{
    procedure SetPaymentTerms(var MyDoc: Record MyDocumentEntity; PaymentTermsCode: Code[10])
    begin
        [|MyDoc."Payment Terms Code"|] := PaymentTermsCode;
        [|MyDoc."Due Date"|] := Today();
    end;
}
