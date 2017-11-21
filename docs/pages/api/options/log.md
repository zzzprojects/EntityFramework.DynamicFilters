---
permalink: log
---

## Definition
Gets or sets an action to `log` all database event as soon as they happen.

{% include template-example.html %} 
{% highlight csharp %}
StringBuilder logger = new StringBuilder();

context.BulkSaveChanges(options =>
{
	options.Log += s => logger.AppendLine(s);
});
{% endhighlight %}

## Purpose
Getting database `log` can often be useful for debugging and see what has been executed under the hood by the library.