import csharp

from NamespaceImport imp
where not exists (ImportUsage u | u.getImport() = imp)
select imp, "This import is not used."
