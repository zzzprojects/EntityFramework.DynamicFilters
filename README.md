Dynamic Global Filters for Entity Framework
===========================================

Create global and scoped filters for Entity Framework queries.  The filters are automatically applied to every query and can be used to support use cases such as Multi-Tenancy, Soft Deletes, Active/Inactive, etc.

Filters can be created using boolean linq expressions and also support the Contains() operator.

Access to DynamicFilters is done via extension methods in the EntityFramework.DynamicFilters namespace on the DbContext and DbModelBuilder classes.


Installation
-----------------------
The package is also available on NuGet: [EntityFramework.DynamicFilters](https://www.nuget.org/packages/EntityFramework.DynamicFilters).


Defining Filters
-----------------------

Filters are fined in DbContext.OnModelCreating().  All filters have global scope and will be used by all DbContexts.  Each DbContext can also choose to provide a "scoped" filter value or can disable the filter via the DisableFilter() extension method.  Scoped parameter changes and filter disabling will apply only to that DbContext and do not affect any existing or future DbContexts.

Filters can be defined on a specific entity class or an interface.  Below is an example of a "soft delete" filter created on an ISoftDelete interface.  This filter will apply to any entity that implements ISoftDelete and will automatically filter those entities by applying the condition "IsDeleted==false".

```csharp
modelBuilder.Filter("IsDeleted", (ISoftDelete d) => d.IsDeleted, false);
```

Filter values can also be provided via a delegate/Func<object> instead of a specific value (as shown in the above example).  This can allow you to vary the parameter value dynamically.  For example, a filter can be created on the UserID and be provided per http request.  Below is an example that obtains a "Person ID" from the Thread.CurrentPrincipal.  This delegate will be evaluated each time the query is executed so it will obtain the "Person ID" associated with each request.
```csharp
modelBuilder.Filter("Notes_CurrentUser", (Note n) => n.PersonID, () => GetPersonIDFromPrincipal(Thread.CurrentPrincipal));
```
In this example, the Note entity is "owned" by the current user.  This filter will ensure that all queries made for Note entities will always be restricted to the current user and it will not be possible for users to retrieve notes for other users.

Linq Filters
-----------------------

Filters can also be created using linq conditions and with multiple parameters.

This Filter() command creates a filter that limits BlogEntry records by AccountID and an IsDeleted flag.  A parameter is created for each condition with parameter names "accountID" and "isDeleted":
```csharp
modelBuilder.Filter("BlogEntryFilter", 
                    (BlogEntry b, Guid accountID, bool isDeleted) => (b.AccountID == accountID) && (b.IsDeleted == isDeleted), 
                    () => GetPersonIDFromPrincipal(Thread.CurrentPrincipal),
                    () => false);
```

The linq syntax is somewhat limited to boolean expressions but does support the Contains() operator on IEnumerable<<T>> to generate sql "in" clauses:
```csharp
var values = new List<int> { 1, 2, 3, 4, 5 };
modelBuilder.Filter("ContainsTest", (BlogEntry b, List<int> valueList) => valueList.Contains(b.IntValue.Value), () => values);
```

If you require support for additional linq operators, please create an [issue](https://github.com/jcachat/EntityFramework.DynamicFilters/issues).

Changing Filter Parameter Values
------------------------------
Within a single DbContext instance, filter parameter values can also be changed.  These changes are scoped to only that DbContext instance and do not affect any other DbContext instances.

To change the Soft Delete filter shown above to return only deleted records, you could do this:
```csharp
context.SetFilterScopedParameterValue("IsDeleted", true);
```

If the filter contains multiple parameters, you must specify the name of the parameter to change like this:
```csharp
context.SetFilterScopedParameterValue("BlogEntryFilter", "accountID", 12345);
```

Global parameter values can also be changed using the SetFilterGlobalParameterValue extension method.


Disabling Filters
------------------------------
To disable s filter, use the DisableFilter extension method like this:
```csharp
context.DisableFilter("IsDeleted");
```

