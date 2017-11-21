---
permalink: temporary-table-batch-by-table
---

## Definition
Gets or sets the number of batches a temporary table can contain. This option may create multiple temporary tables when the number of batches to execute exceed the limit specified.

{% include template-example.html %} 
{% highlight csharp %}
context.BulkSaveChanges(options =>
{
   options.TemporaryTableBatchByTable = 0; // unlimited
});
{% endhighlight %}

## Purpose
So far, we have not found any scenario that could require it. But we still support this option!