import csharp

/** Find unused methods */
from Method m
where not m.isExtern() and
      not m.isUsed()
select m, "This method is unused and may be removed to improve maintainability."
