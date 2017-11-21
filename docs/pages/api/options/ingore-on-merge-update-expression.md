---
permalink: ignore-on-merge-update-expression
---

## Definition
Gets or sets columns to ignore when the `BulkMerge` method execute the `update` statement.

{% include template-example.html %} 
{% highlight csharp %}
context.BulkMerge(list, options => 
        options.IgnoreOnMergeUpdateExpression = entity => new {entity.CreatedDate, entity.CreatedDate}
); 
{% endhighlight %}

## Purpose
The `IgnoreOnMergeUpdateExpression` option let you to ignore some column that should be only `insert.

By example, when may when to `insert` the CreatedDate and CreatedDate but not `update` value.

## Limitations
Database Provider Supported:
- SQL Server
- SQL Azure

_Ask to our support team if you need this option for another provider_