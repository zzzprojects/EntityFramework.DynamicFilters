---
permalink: dbupdateconcurrency-exception
---

## Problem

You execute a method from the Entity Framework Extensions library, and the following error is thrown:

- Type: DbUpdateConcurrencyException

{% include template-exception.html message='Store update, insert, or delete statement affected an unexpected number of rows ([row count]). Entities may have been modified or deleted since entities were loaded. See http://go.microsoft.com/fwlink/?LinkId=472540 for information on understanding and handling optimistic concurrency exceptions.' %}

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
public void BulkSaveChanges_DatabaseWins(DbContext ctx)
{
    bool saveFailed;

    do
    {
        saveFailed = false;

        try
        {
            ctx.BulkSaveChanges();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            saveFailed = true;

            // Update the values of the entity that failed to save from the store 
            ex.Entries.Single().Reload();
        }

    } while (saveFailed); 
}
{% endhighlight %}

#### Client Win
{% highlight csharp %}
public void BulkSaveChanges_ClientWins(DbContext ctx)
{
    bool saveFailed;

    do
    {
        saveFailed = false;

        try
        {
            ctx.BulkSaveChanges();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            saveFailed = true;

            // Update original values from the database 
            var entry = ex.Entries.Single();
            entry.OriginalValues.SetValues(entry.GetDatabaseValues()); 
        }

    } while (saveFailed); 
}
{% endhighlight %}

#### Custom Resolution
{% highlight csharp %}
public void BulkSaveChanges_CustomResolution(CurrentContext ctx)
{

    bool saveFailed;

    do
    {
        saveFailed = false;

        try
        {
            ctx.BulkSaveChanges();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            saveFailed = true;

            // Get the current entity values and the values in the database 
            // as instances of the entity type 
            var entry = ex.Entries.Single();
            var databaseValues = entry.GetDatabaseValues();

            if (entry.Entity is EntitySimple_Concurrency)
            {
                var clientEntity = (EntitySimple_Concurrency) entry.Entity;
                var databaseEntity = (EntitySimple_Concurrency) databaseValues.ToObject();

                // Choose an initial set of resolved values. In this case we 
                // make the default be the values currently in the database. 
                var resolvedEntity = (EntitySimple_Concurrency) databaseValues.ToObject();

                // Have the user choose what the resolved values should be
                resolvedEntity.IntColumn = clientEntity.IntColumn + 100;
                // ... merge all columns...

                // Update the original values with the database values and 
                // the current values with whatever the user choose. 
                entry.OriginalValues.SetValues(databaseValues);
                entry.CurrentValues.SetValues(resolvedEntity);
            }
        }

    } while (saveFailed);
}
{% endhighlight %}
