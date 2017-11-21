---
permalink: temporary-table-schema-name
---

## Definition
Gets or sets the schema name to use for the temporary table.

{% include template-example.html %} 
{% highlight csharp %}
context.BulkSaveChanges(options =>
{
   options.TemporaryTableSchemaName = "zzz";
});
{% endhighlight %}

## Purpose
In a very rare occasion, you may need to specify a schema name. By example, when also using `UsePermanentTable` option.