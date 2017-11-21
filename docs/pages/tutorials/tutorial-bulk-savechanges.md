---
permalink: tutorial-bulk-savechanges
---

## Definition

[BulkSaveChanges](bulk-savechanges) method is the upgraded version of `SaveChanges`.

All changes made in the context are persisted in the database but way faster by reducing the number of database round-trip required!

BulkSaveChanges supports everything:

- Association (One to One, One to Many, Many to Many, etc.)
- Complex Type
- Enum
- Inheritance (TPC, TPH, TPT)
- Navigation Property
- Self-Hierarchy
- Etc.

{% include template-example.html title='BulkSaveChanges Examples' %} 
{% highlight csharp %}
context.Customers.AddRange(listToAdd); // add
context.Customers.RemoveRange(listToRemove); // remove
listToModify.ForEach(x => x.DateModified = DateTime.Now); // modify

// Easy to use
context.BulkSaveChanges();

// Easy to customize
context.BulkSaveChanges(bulk => bulk.BatchSize = 100);
{% endhighlight %}

## Purpose
Using the `ChangeTracker` to detect and persist change automatically is great! However, it leads very fast to some problem when multiples entities need to be saved.

`SaveChanges` method makes a database round-trip for every change. So if you need to insert 10000 entities, then 10000 database round-trip will be performed which is INSANELY slow.

`BulkSaveChanges` work exactly like `SaveChanges` but reduce the number of database round-trips required to greatly helps to improve the performance.

## Performance Comparisons

| Operations      | 1,000 Entities | 2,000 Entities | 5,000 Entities |
| :-------------- | -------------: | -------------: | -------------: |
| SaveChanges     | 1,000 ms       | 2,000 ms       | 5,000 ms       |
| BulkSaveChanges | 90 ms          | 150 ms         | 350 ms         |

## Related Articles

- [How to Benchmark?](benchmark)
- [How to Improve Bulk SaveChanges Performances?](improve-bulk-savechanges)
