import csharp

from Method m
where not m.isExtern() and not m.isUsed()
select m, "This method is unused and can be removed for better maintainability."
