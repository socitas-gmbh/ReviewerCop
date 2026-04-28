page 50200 "My Page"
{
    actions
    {
        area(Processing)
        {
            action([|CarrierSetup|])
            {
                Caption = 'Carrier Setup';
                Image = Setup;
                Promoted = true;
                PromotedOnly = true;
                RunObject = page "Shipping Agents";
                ToolTip = 'Opens the shipping agents list.';
            }
        }
    }
}
