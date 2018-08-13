# Filter

## Definition

Filter add a filter to a single entity. You can filter the query with a predicate to exclude certain data. Filter values can be provided in the following different ways:

 - Specific Value
 - Delegate Expressions
 - Parameter Expressions
 - LINQ Filters

### Specific Value

Filters can be defined on a specific entity class or an interface by providing a specific value, e.g. an **IsDeleted** filter created on an **ISoftDelete** interface which will automatically filter those entities by applying the condition "IsDeleted==false".


```csharp

modelBuilder.Filter("IsDeleted", (ISoftDelete d) => d.IsDeleted, false);

```

This filter will apply to all classes that implements ISoftDelete.

### Delegate Expressions

Filter values can also be provided via a delegate/expression instead of a specific value which allows you to change the parameter value dynamically. For example, a filter can be created on the UserID and provided per HTTP request.


```csharp

modelBuilder.Filter("Notes_CurrentUser", (Note n) => 
			n.PersonID, () => GetPersonIDFromPrincipal(Thread.CurrentPrincipal));

```

Each time the query is executed, this delegate will be evaluated to obtain the **PersonID** associated with each request.

### Parameter Expressions

Parameter delegate expressions can be specified as either a Func < object > or a Func<DbContext, object>.


```csharp

// Specified as Func<object>
modelBuilder.Filter("Notes_CurrentUser", (Note n) => 
			n.PersonID, () => GetPersonIDFromPrincipal(Thread.CurrentPrincipal));

// Specified as Func<DbContext, object>
modelBuilder.Filter("Notes_CurrentUser", (Note n) => 
			n.PersonID, (MyContext ctx) => ctx.CurrentPersonID);

```

In the latest declaration, the parameter value is set to the value of the **CurrentPersonID** property in the current DbContext instance.

### LINQ Filters

Filters can also be created using linq conditions and with multiple parameters.

The following command creates a filter that limits BlogEntry records by **AccountID** and an **IsDeleted** flag.


```csharp

modelBuilder.Filter("BlogEntryFilter", 
			(BlogEntry b, Guid accountID, bool isDeleted) => 
			(b.AccountID == accountID) && (b.IsDeleted == isDeleted), () => 
			GetPersonIDFromPrincipal(Thread.CurrentPrincipal), () => false);

```

A parameter is created for each condition with parameter names "accountID" and "isDeleted".

The LINQ syntax support the Contains() operator on Enumerable<T> to generate SQL "in" clauses:


```csharp

var values = new List<int> { 1, 2, 3, 4, 5 };
modelBuilder.Filter("ContainsTest", (BlogEntry b, List<int> valueList) => 
				valueList.Contains(b.IntValue.Value), () => values);

```
