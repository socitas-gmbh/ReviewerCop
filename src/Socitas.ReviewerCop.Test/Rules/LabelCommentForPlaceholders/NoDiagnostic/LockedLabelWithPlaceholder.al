codeunit 50303 LockedLabelWithPlaceholderTest
{
    procedure GetEndpoint(OrderNumber: Text[50])
    var
        [||]GetLabelEndpointTok: Label '/wso/getlabel?order_number=%1&format=%2', Locked = true;
        Endpoint: Text;
    begin
        Endpoint := StrSubstNo(GetLabelEndpointTok, OrderNumber, 'pdf');
    end;
}
