import csharp

/**
 * Finds empty catch blocks in C# code that may suppress exceptions silently.
 * Empty catch blocks harm reliability by swallowing critical errors.
 */
from CatchClause c
where c.getBody().getASuccessor() = null
select c, "This catch block is empty. Consider logging or rethrowing the exception to improve reliability."
