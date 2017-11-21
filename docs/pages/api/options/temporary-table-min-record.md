---
permalink: temporary-table-min-record
---

## Definition
Gets or sets the minimum number of records to use a temporary table instead of using SQL derived table.

{% include template-example.html %} 
{% highlight csharp %}
context.BulkSaveChanges(options =>
{
   options.TemporaryTableMinRecord = 25;
});
{% endhighlight %}

## Purpose
Our library is smart but finding the `META` number is very hard since there is a lot of factors. Increasing the default value may improve the performance.