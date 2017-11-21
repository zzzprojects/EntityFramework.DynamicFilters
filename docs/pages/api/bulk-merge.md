---
permalink: bulk-merge
---

## Definition
`MERGE` all entities from the database.

A merge is an `UPSERT` operation. All rows that match the entity key are considered as existing and are `UPDATED`, other rows are considered as new rows and are `INSERTED` in the database. 

{% include template-example.html %} 
{% highlight csharp %}
// Easy to use
ctx.BulkMerge(list);

// Easy to customize
context.BulkMerge(customers, options => options.ColumnPrimaryKeyExpression = customer => customer.Code);
{% endhighlight %}

## Purpose
`Merging` entities using a custom key from file importation is a typical scenario.

Despite the `ChangeTracker` being outstanding to track what's modified, it lacks in term of scalability and flexibility.

`SaveChanges` require one database round-trip for every entity to `insert` or `update`. So if you need to `insert` or `update` 10000 entities, then 10000 database round-trip will be performed which is **INSANELY** slow.

`BulkMerge` in counterpart offer great customization and require the minimum database round-trip as possible.

## Performance Comparisons

| Operations      | 1,000 Entities | 2,000 Entities | 5,000 Entities |
| :-------------- | -------------: | -------------: | -------------: |
| SaveChanges     | 1,000 ms       | 2,000 ms       | 5,000 ms       |
| BulkMerge       | 65 ms          | 80 ms          | 110 ms         |

## FAQ

### How can I specify more than one option?
You can specify more than one option using anonymous block.

{% include template-example.html %} 
{% highlight csharp %}
context.BulkMerge(list, options => {
	options.BatchSize = 100);
	options.ColumnInputExpression = c => new {c.ID, c.Name, c.Description});
});
{% endhighlight %}

### How can I specify the Batch Size?
You can specify a custom batch size using the `BatchSize` option.

Read more: [BatchSize](/batch-size)

{% include template-example.html %} 
{% highlight csharp %}
context.BulkMerge(list, options => options.BatchSize = 100);
{% endhighlight %}

### How can I specify custom columns to Merge?
You can specify custom columns using the `ColumnInputExpression` option.

Read more: [ColumnInputExpression](/column-input-expression)

{% include template-example.html %} 
{% highlight csharp %}
context.BulkMerge(list, options => options.ColumnInputExpression = c => new {c.Name, c.Description});
{% endhighlight %}

### How can I specify custom columns to exclude on insert or update?
You can specify custom columns to exclude using the `IgnoreOnMergeInsertExpression` and `IgnoreOnMergeUpdateExpression` option.

Read more: [IgnoreOnMergeInsertExpression](/ignore-on-merge-insert-expression)

Read more: [IgnoreOnMergeUpdateExpression](/ignore-on-merge-update-expression)

{% include template-example.html %} 
{% highlight csharp %}
context.BulkMerge(list, options =>
	{
		options.IgnoreOnMergeInsertExpression = customer => new { customer.UpdatedDate, customer.UpdatedUser };
		options.IgnoreOnMergeUpdateExpression = customer => customer.Code, customer.Col2;
	});
{% endhighlight %}

### How can I specify custom keys to use?
You can specify custom key using the `ColumnPrimaryKeyExpression` option.

Read more: [ColumnPrimaryKeyExpression](/column-primary-key-expression)

{% include template-example.html %} 
{% highlight csharp %}
// Single Key
context.BulkMerge(customers, options => options.ColumnPrimaryKeyExpression = customer => customer.Code);

// Surrogate Key
context.BulkMerge(customers, options => options.ColumnPrimaryKeyExpression = customer => new { customer.Code1, customer.Code2 });
{% endhighlight %}

### How can I include child entities (Entity Graph)?
You can include child entities using the `IncludeGraph` option. Make sure to read about the `IncludeGraph` since this option is not as trivial as others.

Read more: [IncludeGraph](/include-graph)

{% include template-example.html %} 
{% highlight csharp %}
context.BulkMerge(list, options => options.IncludeGraph = true);
{% endhighlight %}

### Why BulkMerge doesn't use the ChangeTracker?
To provide the best performance as possible!

Since using the `ChangeTracker` can greatly reduce performance, we choose to let `BulkSaveChanges` method to handle scenario with `ChangeTracker` and `BulkMerge` scenario without it.

## Related Articles
- [How to Benchmark?](benchmark)
- [How to use Custom Column?](custom-column)
- [How to use Custom Key?](custom-key)
