import csharp

from CatchClause c
where c.getBody().getASuccessor() = null
select c, "This catch block is empty. Consider logging or rethrowing the exception to improve reliability."
