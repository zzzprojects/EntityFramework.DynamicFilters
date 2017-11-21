---
permalink: bulk-synchronize
---

## Definition
`SYNCHRONIZE` all entities from the database.

A synchronize is a mirror operation from the data source to the database. All rows that match the entity key are `UPDATED`, non-matching row that exists from the source are `INSERTED`, non-matching row that exists in the database are `DELETED`.

The database table becomes a mirror of the entity list provided.

{% include template-example.html %} 
{% highlight csharp %}
// Easy to use
ctx.BulkSynchronize(list);

// Easy to customize
context.BulkSynchronize(customers, options => options.ColumnPrimaryKeyExpression = customer => customer.Code);
{% endhighlight %}

## Purpose
`Synchronizing` entities with the database is a very rare scenario, but it may happen when two databases need to be synchronized.

`BulkSynchronize` give you the scalability and flexibility required when if you encounter this situation.

## Performance Comparisons

| Operations      | 1,000 Entities | 2,000 Entities | 5,000 Entities |
| :-------------- | -------------: | -------------: | -------------: |
| SaveChanges     | 1,000 ms       | 2,000 ms       | 5,000 ms       |
| BulkSynchronize | 55 ms          | 65 ms          | 85 ms          |

## FAQ

### How can I specify more than one option?
You can specify more than one option using anonymous block.

{% include template-example.html %} 
{% highlight csharp %}
context.BulkSynchronize(list, options => {
	options.BatchSize = 100);
	options.ColumnInputExpression = c => new {c.ID, c.Name, c.Description});
});
{% endhighlight %}

### How can I specify the Batch Size?
You can specify a custom batch size using the `BatchSize` option.

Read more: [BatchSize](/batch-size)

{% include template-example.html %} 
{% highlight csharp %}
context.BulkSynchronize(list, options => options.BatchSize = 100);
{% endhighlight %}

### How can I specify custom columns to Synchronize?
You can specify custom columns using the `ColumnInputExpression` option.

Read more: [ColumnInputExpression](/column-input-expression)

{% include template-example.html %} 
{% highlight csharp %}
context.BulkSynchronize(list, options => options.ColumnInputExpression = c => new {c.Name, c.Description});
{% endhighlight %}

### How can I specify custom keys to use?
You can specify custom key using the `ColumnPrimaryKeyExpression` option.

Read more: [ColumnPrimaryKeyExpression](/column-primary-key-expression)

{% include template-example.html %} 
{% highlight csharp %}
// Single Key
context.BulkSynchronize(customers, options => options.ColumnPrimaryKeyExpression = customer => customer.Code);

// Surrogate Key
context.BulkSynchronize(customers, options => options.ColumnPrimaryKeyExpression = customer => new { customer.Code1, customer.Code2 });
{% endhighlight %}

## Related Articles
- [How to Benchmark?](benchmark)
- [How to use Custom Column?](custom-column)
- [How to use Custom Key?](custom-key)