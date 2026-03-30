codeunit 50215 MySingleInstanceCodeunit
{
    SingleInstance = true;

    var
        CachedValue: Boolean;
        IsCached: Boolean;

    procedure [||]GetCachedValue(): Boolean
    begin
        if IsCached then
            exit(CachedValue);

        CachedValue := true;
        IsCached := true;
        exit(CachedValue);
    end;
}
