# EnableFilter

## Definition

EnableFilter selectively enable the filter which is globally disabled. Enabling a globally disabled filter will apply only to that DbContext and it will not affect any other DbContext instances.


```csharp

context.EnableFilter("IsDeleted");

```

## Conditionally Enabling Filters

Filters can also be enabled conditionally and you will even need to define those conditions along with your filter definition.


```csharp

modelBuilder.Filter("BlogEntryFilter", (BlogEntry b, Guid accountID) => (b.AccountID == accountID), 
                    () => GetPersonIDFromPrincipal(Thread.CurrentPrincipal));
modelBuilder.EnableFilter("BlogEntryFilter", () => !UserIsAdmin(Thread.CurrentPrincipal));

```

In this example, a filter is defined on BlogEntry records to restrict only the current AccountID. But the filter will only be enabled if the user is not an Admin user.


