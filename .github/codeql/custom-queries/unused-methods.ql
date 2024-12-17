import csharp
import semmle.code.csharp.dataflow.DataFlow

/** Find unused methods in the project */
from Method m
where not m.isExtern() and
      not m.isUsed()
select m, "This method is unused and may be removed to improve maintainability."
