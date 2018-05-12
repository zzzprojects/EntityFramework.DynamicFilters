# EnableAllFilters

## Definition

EnableAllFilters method enables all filters within a DbContext at once which are globally disabled. 


```csharp

context.EnableAllFilters();

```

Enabling a globally disabled filters will apply only to that DbContext and it will not affect any other DbContext instances.

