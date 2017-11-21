---
permalink: temporary-table-use-table-lock
---

## Definition
Gets or sets if the temporary table must be locked when inserting records into it.

{% include template-example.html %} 
{% highlight csharp %}
context.BulkSaveChanges(options =>
{
   options.TemporaryTableUseTableLock = true;
});
{% endhighlight %}

## Purpose
Using table lock increase the overall performance when inserting into a temporary table. This option should not be disabled.