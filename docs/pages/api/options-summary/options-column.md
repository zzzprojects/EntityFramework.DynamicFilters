---
permalink: column
---

- [Column Input](#column-input)
- [Column Output](#column-output)
- [Column InputOutput](#column-inputoutput)
- [Column Primary Key](#column-primary-key)
- [Column Ignore On Merge Insert](#ignore-on-merge-insert)
- [Column Ignore On Merge Update](#ignore-on-merge-update)

--- 

## Column Input
Gets or sets columns to map with the direction `Input`.

The key is required for operation such as `BulkUpdate` and `BulkMerge`.

Read more: [Column Input](column-input-expression)

{% include template-example.html %} 
{% highlight csharp %}
context.BulkMerge(list, options => 
        options.ColumnInputExpression = entity => new {entity.ID, entity.Code}
); 
{% endhighlight %}

## Column Output
Gets or sets columns to map with the direction `Output`.

Read more: [Column Output](column-output-expression)

{% include template-example.html %} 
{% highlight csharp %}
context.BulkMerge(list, options => 
        options.ColumnOutputExpression = entity => new {entity.ModifiedDate, entity.ModifiedUser}
); 
{% endhighlight %}

## Column InputOutput
Gets or sets columns to map with the direction `InputOutput`.

The key is required for operation such as `BulkUpdate` and `BulkMerge`.

Read more: [Column InputOutput](column-input-output-expression)

{% include template-example.html %} 
{% highlight csharp %}
context.BulkMerge(list, options => 
        options.ColumnInputOutputExpression = entity => new {entity.ID, entity.Code}
); 
{% endhighlight %}

## Column Primary Key
Gets or sets columns to use as the `key` for the operation.

Read more: [Column Primary Key](column-primary-key-expression)

{% include template-example.html %} 
{% highlight csharp %}
context.BulkMerge(list, options => 
        options.ColumnPrimaryKeyExpression = entity => new { entity.Code1, entity.Code2 }
); 
{% endhighlight %}

## Ignore On Merge Insert
Gets or sets columns to ignore when the `BulkMerge` method execute the `insert` statement.

Read more: [Column Ignore on Merge Insert](ignore-on-merge-update-expression)

{% include template-example.html %} 
{% highlight csharp %}
context.BulkMerge(list, options => 
        options.IgnoreOnMergeUpdateExpression = entity => new {entity.ModifiedDate, entity.ModifiedUser}
); 
{% endhighlight %}

## Ignore On Merge Update
Gets or sets columns to ignore when the `BulkMerge` method execute the `update` statement.

Read more: [Column Ignore on Merge Update](ignore-on-merge-update-expression)

{% include template-example.html %} 
{% highlight csharp %}
context.BulkMerge(list, options => 
        options.IgnoreOnMergeUpdateExpression = entity => new {entity.CreatedDate, entity.CreatedUser}
); 
{% endhighlight %}