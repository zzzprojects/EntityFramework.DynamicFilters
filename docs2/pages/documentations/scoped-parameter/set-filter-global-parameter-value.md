# SetFilterGlobalParameterValue

## Definition

SetFilterGlobalParameterValue sets the parameter value for a filter with global scope.  If a scoped parameter value is not found, this value will be used.


```csharp

//Change Soft Delete filter to return only deleted records
context.SetFilterGlobalParameterValue("IsDeleted", true);

//For multiple parameters, you must specify the name of the parameter
context.SetFilterGlobalParameterValue("BlogEntryFilter", "accountID", 12345);

```

Parameter values can be set to a specific value as shown in the above example, but it can also be set to a delegate expression (**Func < object >** or **Func<DbContext, object>**).


```csharp

context.SetFilterGlobalParameterValue("EntityCFilter", "status", (TestContext ctx) => ctx.Status);

```
