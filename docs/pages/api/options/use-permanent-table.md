---
permalink: use-permanent-table
---

## Definition
Gets or sets if the library should `create` and `drop` a permanent table instead of using a temporary table.

{% include template-example.html %} 
{% highlight csharp %}
context.BulkSaveChanges(options =>
{
   options.UsePermanentTable = true;
});
{% endhighlight %}

## Purpose
This option can be useful when for some rare reason, you don't have access to the `tempdb` database.