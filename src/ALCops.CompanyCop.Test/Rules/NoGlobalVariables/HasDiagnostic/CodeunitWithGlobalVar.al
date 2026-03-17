codeunit 50200 MyCodeunitWithGlobals
{
    var
        [|MyCounter|]: Integer;
        [|CustomerName|]: Text[100];

    procedure Increment()
    begin
        MyCounter += 1;
    end;

    procedure GetName(): Text[100]
    begin
        exit(CustomerName);
    end;
}
