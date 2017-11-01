## Library Powered By

This library is powered by [Entity Framework Extensions](http://entityframework-extensions.net/?z=github&y=entityframework-dynamicfilters)

<a href="http://entityframework-extensions.net/?z=github&y=entityframework-dynamicfilters">
<kbd>
<img src="https://zzzprojects.github.io/images/logo/entityframework-extensions-pub.jpg" alt="Entity Framework Extensions" />
</kbd>
</a>

# Dynamic Global Filters for Entity Framework

Create global and scoped filters for Entity Framework queries.  The filters are automatically applied to every query and can be used to support use cases such as Multi-Tenancy, Soft Deletes, Active/Inactive, etc.

Filters can be created using boolean linq expressions and also support the Contains() operator.

Access to DynamicFilters is done via extension methods in the EntityFramework.DynamicFilters namespace on the DbContext and DbModelBuilder classes.

Supports MS SQL Server (including Azure), MySQL, Oracle (*see notes below), and PostgreSQL.

## Changes in Version 2
* Added support for creating filters that reference child classes/navigation properties.  See [Issue #65](https://github.com/jcachat/EntityFramework.DynamicFilters/issues/65) for more details.  Requires that FK properties are defined on the models.  Also includes support for Any() and All() on child collections.
* Filter parameter values can now reference the current DbContext instance.  See [Parameter Expressions](#parameter-expressions).
* Filters can be enabled conditionally via a delegate just like parameter values.  See [Conditionally Enabling Filters](#conditionally-enabling-filters).
* Added options to filters to control how/when they are applied to entities.  See [Filter Options](#filter-options)

Putting this all together, you can now do the following:
```csharp
modelBuilder.Filter("NotesForCompany", (Note n, int orgID) => n.Person.OrganizationID==orgID, (MyContext ctx) => ctx.CurrentOrganizationID);
modelBuilder.EnableFilter("UserOrg", (MyContext ctx) => !ctx.UserIsAdmin);
```
This will create a filter to restrict queries on the Notes table to only those records made by people in the current users Organization.  The filter will only be enabled if the current user is not an Administrator.  And both expressions access properties in the current DbContext instance.

## Installation
The package is also available on NuGet: [EntityFramework.DynamicFilters](https://www.nuget.org/packages/EntityFramework.DynamicFilters).


## Defining Filters

Filters are defined in DbContext.OnModelCreating().  

Filters should always follow any other model configuration - including the call to the base.OnModelCreating() method.  It is best to make the filter definitions the final step of OnModelCreating() to make sure that they are not in effect until the entire model is fully configured.

All filters have global scope and will be used by all DbContexts.  Each DbContext can also choose to provide a "scoped" filter value or can disable the filter via the DisableFilter() extension method.  Scoped parameter changes and filter disabling will apply only to that DbContext and do not affect any existing or future DbContexts.

Filters can be defined on a specific entity class or an interface.  Below is an example of a "soft delete" filter created on an ISoftDelete interface.  This filter will apply to any entity that implements ISoftDelete and will automatically filter those entities by applying the condition "IsDeleted==false".

```csharp
modelBuilder.Filter("IsDeleted", (ISoftDelete d) => d.IsDeleted, false);
```

Filter values can also be provided via a delegate/expression instead of a specific value (as shown in the above example).  This can allow you to vary the parameter value dynamically.  For example, a filter can be created on the UserID and be provided per http request.  Below is an example that obtains a "Person ID" from the Thread.CurrentPrincipal.  This delegate will be evaluated each time the query is executed so it will obtain the "Person ID" associated with each request.
```csharp
modelBuilder.Filter("Notes_CurrentUser", (Note n) => n.PersonID, () => GetPersonIDFromPrincipal(Thread.CurrentPrincipal));
```
In this example, the Note entity is "owned" by the current user.  This filter will ensure that all queries made for Note entities will always be restricted to the current user and it will not be possible for users to retrieve notes for other users.

### Parameter Expressions
As of Version 2, parameter delegate expressions can be specified as either a `Func<object>` (as shown above) or a `Func<DbContext, object>` like this:
```csharp
modelBuilder.Filter("Notes_CurrentUser", (Note n) => n.PersonID, (MyContext ctx) => ctx.CurrentPersonID);
```
This allows the parameter value expressions to reference the current DbContext instance.  In this example, the value of the parameter will be set to the value of the CurrentPersonID property in the current MyContext instance.

## Linq Filters

Filters can also be created using linq conditions and with multiple parameters.

This Filter() command creates a filter that limits BlogEntry records by AccountID and an IsDeleted flag.  A parameter is created for each condition with parameter names "accountID" and "isDeleted":
```csharp
modelBuilder.Filter("BlogEntryFilter", 
                    (BlogEntry b, Guid accountID, bool isDeleted) => (b.AccountID == accountID) && (b.IsDeleted == isDeleted), 
                    () => GetPersonIDFromPrincipal(Thread.CurrentPrincipal),
                    () => false);
```

The linq syntax is somewhat limited to boolean expressions but does support the Contains() operator on `Enumerable<T>` to generate sql "in" clauses:
```csharp
var values = new List<int> { 1, 2, 3, 4, 5 };
modelBuilder.Filter("ContainsTest", (BlogEntry b, List<int> valueList) => valueList.Contains(b.IntValue.Value), () => values);
```

If you require support for additional linq operators, please create an [issue](https://github.com/jcachat/EntityFramework.DynamicFilters/issues).

## Changing Filter Parameter Values
Within a single DbContext instance, filter parameter values can also be changed.  These changes are scoped to only that DbContext instance and do not affect any other DbContext instances.

To change the Soft Delete filter shown above to return only deleted records, you could do this:
```csharp
context.SetFilterScopedParameterValue("IsDeleted", true);
```

If the filter contains multiple parameters, you must specify the name of the parameter to change like this:
```csharp
context.SetFilterScopedParameterValue("BlogEntryFilter", "accountID", 12345);
```

Parameter values can be set to a specific value or delegate expressions (`Func<object>` or `Func<DbContext, object>`).

Global parameter values can also be changed using the SetFilterGlobalParameterValue extension methods.


## Enabling and Disabling Filters
To disable a filter, use the DisableFilter extension method like this:
```csharp
context.DisableFilter("IsDeleted");
```

Filters can also be globally disabled after they are created in OnModelCreating:
```csharp
modelBuilder.DisableFilterGlobally("IsDeleted");
```

Globally disabled filters can then be selectively enabled as needed.  Enabling a globally disabled filter will apply only to that DbContext just like scoped parameter values.
```csharp
context.EnableFilter("IsDeleted");
```

You can also mass enable/disable all filters within a DbContext at once:
```
context.DisableAllFilters();
context.EnableAllFilters();
```

However, note that if a query is executed with a filter disabled, Entity Framework will cache those entities internally.  If you then enable a filter, cached entities may be included in child collections that otherwise should not be.  Entity Framework caches per DbContext so if you find this to be an issue, you can avoid it by using a fresh DbContext.

In order to be able to dynamically enable/disable filters, a special condition is added to the sql query that will look something like:
```
OR (@DynamicFilterParam_000001 IS NOT NULL)
```
If the filter is enabled, this condition is dynamically excluded from the sql just before execution but will be present when the filter is disabled (and the parameter value will be set to 1).  In both cases, the parameter will be listed in the parameter list sent in the query.  

If you will never require the need to enable or disable filters at any time during the application life cycle, you can prevent this condition entirely using these 2 methods:
```
modelBuilder.PreventDisabledFilterConditions("IsDeleted");  // disable a single filter
modelBuilder.PreventAllDisabledFilterConditions();          // disable all filters defined up to calling this method
```
This can only be done during OnModelCreating and once turned off, can not be turned back on.  This is because we only have 1 opportunity to include this condition in the query so once the query is compiled, we cannot change it.

In most cases, this condition should not affect query performance at all - especially since we exclude it when the filter is enabled.  But if additional conditions are used in the where clause and a multi-column index is involved, this condition may cause SQL Server to choose the wrong index or perform a table scan.  You should examine the performance of your queries and index usage to determine if this is an issue for you.

### Conditionally Enabling Filters
As of Version 2, filters can also be enabled conditionally by specifying a delegate expression that will be evaluated at query execution.  This can be used to only enable a filter under specific conditions and to define those conditions along with your filter definition.

For example:
```csharp
modelBuilder.Filter("BlogEntryFilter", (BlogEntry b, Guid accountID) => (b.AccountID == accountID), 
                    () => GetPersonIDFromPrincipal(Thread.CurrentPrincipal));
modelBuilder.EnableFilter("BlogEntryFilter", () => !UserIsAdmin(Thread.CurrentPrincipal));
```
creates a filter on BlogEntry records to restrict to only the current uses AccountID.  But the filter will only be enabled if the user is not an Admin user.

This expression can be specified as either a `Func<object>` or a `Func<DbContext, object>` expression just like parameter value expressions.

### Filter Options
As of Version 2.9, fluent-style options have been added to allow you to control how filters are applied to entities.  The following options are available:

* SelectEntityTypeCondition: Allows you to specify a delegate that will be called for each Type that is found to match the filter.  You can inspect the type and return true/false to indicate if this filter should be applied to this specific Type.  This allows you create a filter on an Interface but then not apply it to specific entities. 

    The following example creates a filter on all entities implementing ISoftDelete except for entities implementing the IExceptMe interface.
   ```
   modelBuilder.Filter("ISoftDelete", (ISoftDelete d, bool isDeleted) => d.IsDeleted == isDeleted, false, opt => opt.SelectEntityTypeCondition(type => !typeof(IExceptMe).IsAssignableFrom(type)));
   ```
   
* ApplyToChildProperties: When set to false, the filter will only be applied to the main entity.  It will not be applied to any child properties.

   Default is true (filter is applied to all entities in the query).
   
   The following filter will only be applied to the main entity of a query - not to any child properties:
   ```
   modelBuilder.Filter("ISoftDelete", (ISoftDelete d, bool isDeleted) => d.IsDeleted == isDeleted, false, opt => opt.ApplyToChildProperties(false));
   ```

* ApplyRecursively: When set to false, the filter will not be applied recursively.  Once it has been applied to a parent entity, it will not be applied again.  If an entity contains 2 child properties that match the same filter, it will be applied to both properties.  But it would then not be applied to any child properties of either of those entities.
   
   Default is true - all filters are applied recursively.
   
   The following filter will not be applied recursively:
   ```
   modelBuilder.Filter("ISoftDelete", (ISoftDelete d, bool isDeleted) => d.IsDeleted == isDeleted, false, opt => opt.ApplyRecursively(false));
   ```


## Oracle Support
Oracle is supported using the [Official Oracle ODP.NET, Managed Entity Framework Driver](https://www.nuget.org/packages/Oracle.ManagedDataAccess.EntityFramework) with the following limitations:
* The Oracle driver does not support generating an "in" expression.  Using the "Contains" operator will result in outputting a series of equals/or expressions.
* Using a DateTime value tends to throw an exception saying "The member with identity 'Precision' does not exist in the metadata collection."  This seems to be a bug in the Oracle driver.  Using a DateTimeOffset instead of a DateTime works correctly (which also then uses the Oracle TIMESTAMP datatype instead of DATE).

## SQL Server CE Support
SQL Server CE is supported with the following limitations:
* The SQL Server CE provider does not support modifying the CommandText property during SQL interception.  That is necessary in order to do some of the dynamic parameter value replacements.  This means that Contains(IEnumerable<T>) is not supported on SQL Server CE and will throw an exception.
* SQL Server CE does not support the "like @value+'%'" syntax (see https://stackoverflow.com/questions/1916248/how-to-use-parameter-with-like-in-sql-server-compact-edition).  So string.StartsWith(@value) is not supported and will throw a Format exception.
