EntityFramework.DynamicFilters
==============================

DynamicFilters allow you to create global and scoped filters for Entity Framework queries.  The filters are automatically included in every query and can be used to support use cases such as Multi-Tenancy, Soft Deletes, Active/Inactive, etc.

Access to DynamicFilters is done via extension methods on DbContext and DbModelBuilder which are in the EntityFramework.DynamicFilters namespace.

Configuration
-----------------------
Initialize DynamicFilters in DbContext.OnModelCreating():

```csharp
protected override void OnModelCreating(DbModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Initialize EntityFramework.DynamicFilters
    this.InitializeDynamicFilters();
}
```

Defining Filters
-----------------------

Filters are fined in DbContext.OnModelCreating.  All filters have global scope and will be used by all DbContexts.  Each DbContext can also choose to provide a "scoped" filter value or can disable the filter by setting the value to null.  Scoped parameter changes will apply only to that DbContext and do not affect any existing or future DbContexts.

Filters can be defined on a specific entity class or an interface.  Below is an example of a "soft delete" filter created on an ISoftDelete interface.  This filter will apply to any entity that implements ISoftDelete and will automatically filter those entities by applying the condition "IsDeleted==false".

```csharp
modelBuilder.Filter("IsDeleted", (ISoftDelete d) => d.IsDeleted, false);
```

Filter values can also be provided via a delegate/Func<Object> instead of a specific value (as shown in the above example).  This can allow you to vary the parameter value dynamically.  For example, a filter can be created on the UserID and be provided per http request.  Below is an example that obtains a "Person ID" from the Thread.CurrentPrincipal.  This delegate will be evaluated each time the query is executed so it will obtain the "Person ID" associated with each request.
```csharp
modelBuilder.Filter("Notes_CurrentUser", (Note n) => n.PersonID, () => SessionPrincipal.PersonIDFromPrincipal(Thread.CurrentPrincipal));
```
In this example, the Note entity is "owned" by the current user.  This filter will ensure that all queries made for Note entities will always be restricted to the current user and it will not be possible for users to retrieve notes for other users.


Scoped Filter Parameter Values
------------------------------
Within a single DbContext instance, filter parameter values can also be changed or filters can be disabled.  These changes are scoped to only that DbContext instance and do not affect any other DbContext instances.

To change the Soft Delete filter shown above to return only deleted records, you could do this:
```csharp
context.SetFilterScopedParameterValue("IsDeleted", true);
```

Or to disable that filter completely and return all records regardless of the value of IsDeleted, set the value to null like this:
```csharp
context.SetFilterScopedParameterValue("IsDeleted", null);
```


