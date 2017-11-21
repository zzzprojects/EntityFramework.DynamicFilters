---
permalink: logging
---

- [Log](#log)
- [UseLogDump](#uselogdump)
- [LogDump](#logdump)

---

## Log
Gets or sets an action to `log` all database event as soon as they happen.

Read more: [Log](log)

{% include template-example.html %} 
{% highlight csharp %}
StringBuilder logger = new StringBuilder();

context.BulkSaveChanges(options =>
{
	options.Log += s => logger.AppendLine(s);
});
{% endhighlight %}

---

## UseLogDump
Gets or sets if all `log` related to database event should be stored in a `LogDump` properties.

Read more: [UseLogDump](use-log-dump)

{% include template-example.html %} 
{% highlight csharp %}
StringBuilder logDump;

context.BulkSaveChanges(options =>
{
	options.UseLogDump = true;
	options.BulkOperationExecuted = bulkOperation => logDump = bulkOperation.LogDump;
});
{% endhighlight %}

---

## LogDump
Gets all `logged` database event when `UseLogDump` is enabled.

Read more: [LogDump](log-dump)

{% include template-example.html %} 
{% highlight csharp %}
StringBuilder logDump;

context.BulkSaveChanges(options =>
{
	options.UseLogDump = true;
	options.BulkOperationExecuted = bulkOperation => logDump = bulkOperation.LogDump;
});
{% endhighlight %}
