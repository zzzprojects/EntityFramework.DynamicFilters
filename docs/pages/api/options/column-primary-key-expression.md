---
permalink: column-primary-key-expression
---

## Definition
Gets or sets columns to use as the `key` for the operation.

{% include template-example.html %} 
{% highlight csharp %}
context.BulkMerge(list, options => 
        options.ColumnPrimaryKeyExpression = entity => new { entity.Code1, entity.Code2 }
); 
{% endhighlight %}

## Purpose
The `ColumnPrimaryKeyExpression` option let you choose a specific key to use to perform the bulk operations.

By example, when importing a file, you may have not have access to the `ID` but a unique `Code` instead.