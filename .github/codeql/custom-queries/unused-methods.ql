import csharp

/** 
 * Finds unused methods in the codebase.
 */
from Method m
where not m.isExtern() and not m.isUsed()
select m, "This method is unused and can be removed for better maintainability."
