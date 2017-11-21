---
permalink: column-input-output-expression
---

## Definition
Gets or sets columns to map with the direction `InputOutput`.

The key is required for operation such as `BulkUpdate` and `BulkMerge`.

{% include template-example.html %} 
{% highlight csharp %}
context.BulkMerge(list, options => 
        options.ColumnInputOutputExpression = entity => new {entity.ID, entity.Code}
); 
{% endhighlight %}

## Purpose
The `ColumnInputOutputExpression` option let you choose specific properties in which you want to perform the bulk operations.

By example, when importing a file, you may have not data on all properties.