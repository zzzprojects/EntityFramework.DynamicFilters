---
permalink: tutorial-bulk-operations
---

## Definition
Bulk operation methods give you additional flexibility by allowing to customize options such as primary key, columns, include childs entities and more.

They are also faster than BulkSaveChanges since they don’t use the ChangeTracker and doesn’t call the DetectChanges method.

Bulk Operations Available:
- [BulkInsert](/bulk-insert)
- [BulkUpdate](/bulk-update)
- [BulkDelete](/bulk-delete)
- [BulkMerge](/bulk-merge) (UPSERT operation)
- [BulkSynchronize](/bulk-synchronize)

{% include template-example.html title='Bulk Operations Examples' %} 
{% highlight csharp %}
// Easy to use
context.BulkInsert(list);
context.BulkUpdate(list);
context.BulkDelete(list);
context.BulkMerge(list);

// Easy to customize
context.BulkMerge(customers, bulk => bulk.ColumnPrimaryKeyExpression = customer => customer.Code; });
{% endhighlight %}

## Purpose
Using the ChangeTracker to detect and persist change automatically is great! However, almost every application has some particular scenario which requires some customization and better performance.

By example:
- Inserting thousands of hundreds of data with child entities
- Updating only some particular fields
- Merging a list of customers using the code instead of the key

## Performance Comparisons

| Operations      | 1,000 Entities | 2,000 Entities | 5,000 Entities |
| :-------------- | -------------: | -------------: | -------------: |
| SaveChanges     | 1,000 ms       | 2,000 ms       | 5,000 ms       |
| BulkInsert      | 6 ms           | 10 ms          | 15 ms          |
| BulkUpdate      | 50 ms          | 55 ms          | 65 ms          |
| BulkDelete      | 45 ms          | 50 ms          | 60 ms          |
| BulkMerge       | 65 ms          | 80 ms          | 110 ms         |

### Related Articles

- [How to Benchmark?](benchmark)
- [How to use Custom Column?](custom-column)
- [How to use Custom Key?](custom-key)
