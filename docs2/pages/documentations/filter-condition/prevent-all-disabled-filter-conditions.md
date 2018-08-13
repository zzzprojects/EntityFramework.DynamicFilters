# PreventAllDisabledFilterConditions

## Definition

Prevent the inclusion of conditions in the sql query used to enable/disable filters. This will completely prevent the ability to enable & disable filters globally throughout the application.

If you never require the need to enable or disable filters at any time during the application life cycle, you can prevent this condition entirely.


```csharp

// disable a single filter
modelBuilder.PreventDisabledFilterConditions("IsDeleted");

// disable all filters defined up to calling this method
modelBuilder.PreventAllDisabledFilterConditions();

```

 - Once this is set, it cannot be undone because EF only gives us 1 shot at including those conditions.
 - It will apply to all filters defined at the time this method is called.  
 - So to apply to all filters, call this after they have all been defined.
