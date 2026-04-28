codeunit 50101 TernaryInArgumentTest
{
    procedure ShowStatus(IsActive: Boolean)
    var
        Status: Text[20];
    begin
        [|if|] IsActive then
            Status := 'Active'
        else
            Status := 'Inactive';
        Message('%1', Status);
    end;
}
