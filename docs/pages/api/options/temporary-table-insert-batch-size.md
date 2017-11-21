---
permalink: temporary-table-insert-batch-size
---

## Definition
Gets or sets the number of records to use in a batch when inserting in a temporary table. This number is recommended to be high.

{% include template-example.html %} 
{% highlight csharp %}
context.BulkSaveChanges(options =>
{
   options.TemporaryTableInsertBatchSize = 50000;
});
{% endhighlight %}

## Purpose
Increasing the default value may improve the performance. Since the temporary table doesn't contain index and trigger and it's normally locked during the insert, you may use a very high value.