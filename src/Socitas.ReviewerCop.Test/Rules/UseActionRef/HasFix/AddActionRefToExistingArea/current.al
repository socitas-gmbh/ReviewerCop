page 50200 "My Page"
{
    actions
    {
        area(Processing)
        {
            action(ExistingAction)
            {
                Caption = 'Existing';
            }
            action([|CarrierSetup|])
            {
                Caption = 'Carrier Setup';
                Promoted = true;
                PromotedOnly = true;
                RunObject = page "Shipping Agents";
            }
        }
        area(Promoted)
        {
            actionref(ExistingAction_Promoted; ExistingAction) { }
        }
    }
}
