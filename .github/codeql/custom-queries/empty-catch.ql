import csharp

/**
 * @name Empty Catch Block
 * @id cs-empty-catch
 * @description Finds catch blocks without any statements.
 * @kind path-problem
 * @problem.severity warning
 * @precision high
 */
from CatchClause catch
where catch.getBody().getStmtCount() = 0
select catch, "Avoid empty catch blocks. Add logging or appropriate handling."
