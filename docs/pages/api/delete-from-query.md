---
permalink: delete-from-query
---

## Definition
`DELETE` all rows from the database using a LINQ Query without loading entities in the context.

A `DELETE` statement is built using the LINQ expression and directly executed in the database.

{% include template-example.html %} 
{% highlight csharp %}
// DELETE all customers that are inactive
context.Customers.Where(x => !x.IsActif).DeleteFromQuery();

// DELETE customers by id
context.Customers.Where(x => x.ID == userId).DeleteFromQuery();
{% endhighlight %}

## Purpose
`Deleting` entities using `SaveChanges` normally require to load them first in the `ChangeTracker`. These additional round-trips are often not necessary.

`DeleteFromQuery` give you access to directly execute a `DELETE` statement in the database and provide a **HUGE** performance improvement.

## Performance Comparisons

| Operations      | 1,000 Entities | 2,000 Entities | 5,000 Entities |
| :-------------- | -------------: | -------------: | -------------: |
| SaveChanges     | 1,000 ms       | 2,000 ms       | 5,000 ms       |
| DeleteFromQuery | 1 ms           | 1 ms           | 1 ms           |

## FAQ

### Why DeleteFromQuery is faster than SaveChanges, BulkSaveChanges, and BulkDelete?

`DeleteFromQuery` execute a statement directly in SQL such as `DELETE FROM [TableName] WHERE [Key]`. 

Other operations normally require one or multiple database round-trips which make the performance slower.