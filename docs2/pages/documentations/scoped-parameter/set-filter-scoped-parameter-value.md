# SetFilterScopedParameterValue

SetFilterScopedParameterValue sets the parameter for a filter within the current DbContext scope.  Once the DbContext is disposed, this parameter will no longer be in scope and will be removed.


```csharp

//Change Soft Delete filter to return only deleted records
context.SetFilterScopedParameterValue("IsDeleted", true);

//For multiple parameters, you must specify the name of the parameter
context.SetFilterScopedParameterValue("BlogEntryFilter", "accountID", 12345);

```

Parameter values can be set to a specific value as shown in the above example, but it can also be set to a delegate expression (**Func < object >** or **Func<DbContext, object>**).


```csharp

context.SetFilterScopedParameterValue("EntityCFilter", "status", (TestContext ctx) => ctx.Status);

```
