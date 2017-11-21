---
permalink: ignore-on-merge-insert-expression
---

## Definition
Gets or sets columns to ignore when the `BulkMerge` method execute the `insert` statement.

{% include template-example.html %} 
{% highlight csharp %}
context.BulkMerge(list, options => 
        options.IgnoreOnMergeUpdateExpression = entity => new {entity.ModifiedDate, entity.ModifiedUser}
); 
{% endhighlight %}

## Purpose
The `IgnoreOnMergeInsertExpression` option let you to ignore some column that should be only `updated.

By example, when may when to `update` the ModifiedData and ModifiedUser but not `insert` value.

## Limitations
Database Provider Supported:
- SQL Server
- SQL Azure

_Ask to our support team if you need this option for another provider_