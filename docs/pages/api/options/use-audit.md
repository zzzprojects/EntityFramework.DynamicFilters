---
permalink: use-audit
---

## Definition
Gets or sets if `INSERTED` and `DELETED` data from the database should be returned as `AuditEntries`.

{% include template-example.html %} 
{% highlight csharp %}
List<AuditEntry> auditEntries = new List<AuditEntry>();

context.BulkSaveChanges(list, options =>
{
	options.UseAudit = true;
	options.BulkOperationExecuted = bulkOperation => auditEntries.AddRange(bulkOperation.AuditEntries);
});
{% endhighlight %}

## Purpose
Logging old value and new value is often useful to keep a history of change in the database or file.

## FAQ

### Why enabling this option decrease the performance?
Enabling this option will require additional data to be returned from the database.