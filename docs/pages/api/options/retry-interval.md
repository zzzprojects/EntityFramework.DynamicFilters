---
permalink: retry-interval
---

## Definition
Gets or sets the interval to wait before retrying an operation when a transient error occurs.

{% include template-example.html %} 
{% highlight csharp %}
context.BulkSaveChanges(options => {
	options.RetryCount = 3;
	options.RetryInterval = new TimeSpan(100);
});

{% endhighlight %}

## Purpose
A transient error is a temporary error that is likely to disappear soon. That rarely happens may they may occur!

These options allow reducing a bulk operations fail by making them retry it when a transient error occurs.

## FAQ

### What are transient error code supported?
You can find a list of transient error here: [Transient fault error codes](https://docs.microsoft.com/en-us/azure/sql-database/sql-database-develop-error-messages#transient-fault-error-codes)

Which include common error such as:
- Cannot open database
- The service is currently busy
- Database is not currently available
- Not enough resource to process request