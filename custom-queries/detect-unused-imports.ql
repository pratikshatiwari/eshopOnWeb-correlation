import csharp

from NamespaceImport import
where not exists(
  VariableAccess va |
  va.getType().getNamespace().toString() = import.getImportedNamespace()
)
select import, "This namespace import is not used."
