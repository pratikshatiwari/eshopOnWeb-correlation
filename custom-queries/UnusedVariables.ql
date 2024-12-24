import csharp

/**
 * @name Unused local variable
 * @description Detects local variables declared but never used in the code.
 * @kind problem
 * @tags correctness, maintainability
 * @problem.severity warning
 * @id cs/unusedvariable
 */
from LocalVariable lv
where not exists (
  // Check if the variable is accessed in the code
  lv.getAnAccess()
)
select lv, "The variable '" + lv.getName() + "' is declared but never used."
