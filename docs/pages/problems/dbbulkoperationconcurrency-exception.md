---
permalink: dbbulkoperationconcurrency-exception
---

## Problem

You execute a method from the Entity Framework Extensions library, and the following error is thrown:

- Type: DbBulkOperationConcurrencyException

{% include template-exception.html message='A concurrency exception has occured. Entities may have been modified or deleted since entities were loaded.' %}

## Solution

### Cause

Another thread have already performed the operation.

### Fix

There is three possible resolution:

- Database Win
- Client Win
- Custom Resolution

#### Database Win
{% highlight csharp %}
public void BulkUpdate_DatabaseWins<T>(CurrentContext ctx, List<T> list) where T : class
{
    try
    {
        ctx.BulkUpdate(list);
    }
    catch (DbBulkOperationConcurrencyException ex)
    {
        // DO nothing (or log), keep database values!
    }
}
{% endhighlight %}

#### Client Win
{% highlight csharp %}
public void BulkUpdate_StoreWins<T>(CurrentContext ctx, List<T> list) where T : class
{
    try
    {
        ctx.BulkUpdate(list);
    }
    catch (DbBulkOperationConcurrencyException ex)
    {
        // FORCE update store entities
        ctx.BulkUpdate(list, operation => operation.AllowConcurrency = false);
    }
}
{% endhighlight %}

#### Custom Resolution
{% highlight csharp %}
public void BulkUpdate_CustomResolution<T>(CurrentContext ctx, List<T> list) where T : class
{
    try
    {
        ctx.BulkUpdate(list);
    }
    catch (DbBulkOperationConcurrencyException ex)
    {
        foreach (var entry in ex.Entries)
        {
            ObjectStateEntry objectEntry;

            if (entry is EntitySimple_Concurrency)
            {
                var clientEntry = (EntitySimple_Concurrency) entry;
                var databaseEntry = ctx.EntitySimple_Concurrencys.Single(x => x.ID == clientEntry.ID);

                // merge properties like you want
                clientEntry.IntColumn = databaseEntry.IntColumn + 303;
            }
        }

        // FORCE update store entities
        ctx.BulkUpdate(list, operation => operation.AllowConcurrency = false);
    }
}
{% endhighlight %}
