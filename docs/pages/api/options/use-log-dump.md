---
permalink: use-log-dump
---

## Definition
Gets or sets if all `log` related to database event should be stored in a `LogDump` properties.

{% include template-example.html %} 
{% highlight csharp %}
StringBuilder logDump;

context.BulkSaveChanges(options =>
{
	options.UseLogDump = true;
	options.BulkOperationExecuted = bulkOperation => logDump = bulkOperation.LogDump;
});
{% endhighlight %}

## Purpose
Getting database `log` can often be useful for debugging and see what has been executed under the hood by the library.