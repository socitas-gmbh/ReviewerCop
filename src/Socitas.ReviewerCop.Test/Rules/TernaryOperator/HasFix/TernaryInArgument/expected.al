codeunit 50109 TernaryArgFixTest
{
    procedure ShowStatus(IsActive: Boolean)
    var
        Status: Text[20];
    begin
        Status := IsActive ? 'Active' : 'Inactive';
        Message('%1', Status);
    end;
}
