---
permalink: batch-size
---

## Definition
Gets or sets the number of records to use in a batch.

{% include template-example.html %} 
{% highlight csharp %}
// Instance
context.BulkSaveChanges(options => options.BatchSize = 1000);

// Global
EntityFrameworkManager.BulkOperationBuilder = builder => builder.BatchSize = 1000;
{% endhighlight %}

## Purpose
Having access to modify the `BatchSize` default value may be useful in some occasion which the performance is very affected.

Don't try to optimize it if your application is not affected by some performance problem.

## FAQ

### What's the optimized BatchSize?
Not too low, not too high!

Unfortunately, there is no magic value.

If you set it to low, the library will make to many round-trips may decrease the overall performance.

If you set it to high, the library will make fewer round-trips but could take must time to write on the server which may decrease the overall performance.

The is no perfect number since there is to many factors such as:
- Column Size
- Index
- Latency
- Trigger
- Etc.