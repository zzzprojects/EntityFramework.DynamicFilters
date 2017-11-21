---
permalink: bulk-operation-executed
---

## Definition
Gets or sets an action to execute `after` the bulk operation is executed.

{% include template-example.html %} 
{% highlight csharp %}
context.BulkSaveChanges(options => {
	options.BulkOperationExecuted = bulkOperation => { /* configuration */ };
});
{% endhighlight %}

## Purpose
For some options such as `Audit`, values must be taken directly from the `Bulk Operations` after it's executed. This `event` allows you to take this kind of information.

{% include template-example.html %} 
{% highlight csharp %}
List<AuditEntry> auditEntries = new List<AuditEntry>();

context.BulkSaveChanges(list, options =>
{
	options.UseAudit = true;
	options.BulkOperationExecuted = bulkOperation => auditEntries.AddRange(bulkOperation.AuditEntries);
});
{% endhighlight %}