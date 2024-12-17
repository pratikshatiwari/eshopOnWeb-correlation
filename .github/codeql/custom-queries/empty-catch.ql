import csharp

/** 
 * @name Empty catch block
 * @description Finds empty catch blocks that suppress exceptions silently.
 * @kind path-problem
 * @id cs/empty-catch-block
 * @problem.severity warning
 * @precision high
 * @tags reliability
 *       maintainability
 */

from CatchClause c
where c.getBody().getStmtCount() = 0
select c, "This catch block is empty. Consider adding error handling or logging." 
