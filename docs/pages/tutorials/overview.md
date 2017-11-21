---
permalink: overview
---

## Definition

**Entity Framework Extensions** is a library that dramatically improves EF performances by using bulk and batch operations.

People using this library often report performance enhancement by 50x times and more!

The library is installed through <a href="/installing">NuGet</a>. Extension methods are added automatically to your DbContext.

It easy to use, easy to customize.

{% include template-example.html %} 

{% highlight csharp %}
// Easy to use
context.BulkSaveChanges();
context.BulkInsert(list);
context.BulkUpdate(list);
context.BulkDelete(list);
context.BulkMerge(list);

// Easy to customize
context.BulkMerge(customers, options => options.ColumnPrimaryKeyExpression = customer => customer.Code);
{% endhighlight %}

## Purpose
Entity Framework is reputed to be very slow when saving multiple entities! The performance issue is mainly due to the **DetectChanges** method and the number of database round-trip.

By example for SQL Server, for every entity you save, a database round-trip must be performed. So if you need to insert 10000 entities, then 10000 database round-trip will be performed which is **INSANELY** slow.

Entity Framework Extensions in counterpart only requires a few database round-trip which greatly helps to improve the performance.

## BulkSaveChanges Method

**BulkSaveChanges** method is the upgraded version of **SaveChanges**.

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

### Performance Comparisons

| Operations      | 1,000 Entities | 2,000 Entities | 5,000 Entities |
| :-------------- | -------------: | -------------: | -------------: |
| SaveChanges     | 1,000 ms       | 2,000 ms       | 5,000 ms       |
| BulkSaveChanges | 90 ms          | 150 ms         | 350 ms         |

## Bulk Operations Methods

Bulk operation methods give you additional flexibility by allowing to customize options such as primary key, columns, include childs entities and more.

They are also faster than **BulkSaveChanges** since they don't use the ChangeTracker and doesn't call the **DetectChanges** method.

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

### Performance Comparisons

| Operations      | 1,000 Entities | 2,000 Entities | 5,000 Entities |
| :-------------- | -------------: | -------------: | -------------: |
| SaveChanges     | 1,000 ms       | 2,000 ms       | 5,000 ms       |
| BulkInsert      | 6 ms           | 10 ms          | 15 ms          |
| BulkUpdate      | 50 ms          | 55 ms          | 65 ms          |
| BulkDelete      | 45 ms          | 50 ms          | 60 ms          |
| BulkMerge       | 65 ms          | 80 ms          | 110 ms         |

## Batch Operations Methods

Batch Operations method allow to perform **UPDATE** or **DELETE** operation directly in the database using a LINQ Query without loading entities in the context.

Everything is executed on the database side to let you get the best performance available.

Batch Operations Available:
- [DeleteFromQuery](delete-from-query)
- [UpdateFromQuery](update-from-query)

{% include template-example.html title='Batch Operations Examples' %} 
{% highlight csharp %}
// DELETE all customers that are inactive for more than two years
context.Customers
    .Where(x => x.LastLogin < DateTime.Now.AddYears(-2))
    .DeleteFromQuery();
 
// UPDATE all customers that are inactive for more than two years
context.Customers
    .Where(x => x.Actif && x.LastLogin < DateTime.Now.AddYears(-2))
    .UpdateFromQuery(x => new Customer {Actif = false});
{% endhighlight %}

### Performance Comparisons

| Operations      | 1,000 Entities | 2,000 Entities | 5,000 Entities |
| :-------------- | -------------: | -------------: | -------------: |
| SaveChanges     | 1,000 ms       | 2,000 ms       | 5,000 ms       |
| DeleteFromQuery | 1 ms           | 1 ms           | 1 ms           |
| UpdateFromQuery | 1 ms           | 1 ms           | 1 ms           |
