page 50200 MyPageWithGlobals
{
    var
        [|IsInitialized|]: Boolean;

    trigger OnOpenPage()
    begin
        IsInitialized := true;
    end;
}
