---
permalink: column-output-expression
---

## Definition
Gets or sets columns to map with the direction `Output`.

{% include template-example.html %} 
{% highlight csharp %}
context.BulkMerge(list, options => 
        options.ColumnOutputExpression = entity => new {entity.ModifiedDate, entity.ModifiedUser}
); 
{% endhighlight %}

## Purpose
The `ColumnOutputExpression` option let you choose specific properties in which you want to get retrieve data from the database.