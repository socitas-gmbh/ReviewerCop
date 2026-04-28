page 50201 "Base Page"
{
    actions
    {
        area(Processing)
        {
        }
    }
}

pageextension 50200 "My Page Ext" extends "Base Page"
{
    actions
    {
        addlast(Processing)
        {
            action([|NewAction|])
            {
                Caption = 'New Action';
                Promoted = true;
                PromotedOnly = true;
            }
        }
    }
}
