---
permalink: sql-bulk-copy-options
---

## Definition
Gets or sets the SqlBulkCopyOptions to use when `SqlBulkCopy` is used to directly insert in the destination table.

{% include template-example.html %} 
{% highlight csharp %}
context.BulkSaveChanges(options =>
{
   options.SqlBulkCopyOptions = SqlBulkCopyOptions.Default | SqlBulkCopyOptions.TableLock;
});
{% endhighlight %}

## Purpose
Modifying the SqlBulkCopyOptions to include by example `TableLock` may increase significantly the performance.