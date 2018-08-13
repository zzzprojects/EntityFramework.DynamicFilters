# DisableAllFilters

## Definition

DisableFilter selectively disable the filter which is globally enabled. 


```csharp

context.DisableFilter("IsDeleted");

```

Disabling a globally enabled filter will apply only to that DbContext and it will not affect any other DbContext instances.

## Disable Filter Globally

DisableFilterGlobally disables the filter globally and it can be enabled as needed via DbContext.EnableFilter().


```csharp

modelBuilder.DisableFilterGlobally("IsDeleted");

```
