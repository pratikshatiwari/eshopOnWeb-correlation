import csharp

/**
 * @name Unused Methods
 * @description Find private methods that are never called.
 * @kind problem
 * @id cs-unused-methods
 */

from Method m
where
    m.isPrivate() and
    not exists(Call call | call.getTarget() = m)
select m, "Private method '" + m.getName() + "' is never called and might be dead code."
