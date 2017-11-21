---
permalink: audit-entries
---

## Definition
Gets `INSERTED` and `DELETED` data when `UseAudit` option is enabled.

{% include template-example.html %} 
{% highlight csharp %}
List<AuditEntry> auditEntries = new List<AuditEntry>();

context.BulkSaveChanges(list, options =>
{
	options.UseAudit = true;
	options.BulkOperationExecuted = bulkOperation => auditEntries.AddRange(bulkOperation.AuditEntries);
});

foreach (var entry in auditEntries)
{
    foreach (var value in entry.Values)
    {
        var oldValue = value.OldValue;
        var newValue = value.NewValue;
    }
}
{% endhighlight %}

## Purpose
Logging old value and new value is often useful to keep a history of change in the database or file.

## FAQ

### Why enabling this option decrease the performance?
Enabling this option will require additional data to be returned from the database.