import csharp

/**
 * @name Unused Private Methods
 * @description Detect unused private methods in C# code.
 * @kind problem
 * @id cs-unused-private-methods
 */
from Method m
where m.isPrivate() and not exists(Call call | call.getTarget() = m)
select m, "Unused private method: " + m.getName()
