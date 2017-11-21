---
permalink: batch
---

- [BatchSize](#batchsize)
- [BatchTimeout](#batchtimeout)
- [BatchDelayInterval](#batchDelayInterval)

---

### BatchSize
Gets or sets the number of records to use in a batch.

Read more: [BatchSize](batch-size)

{% include template-example.html %} 
{% highlight csharp %}
context.BulkSaveChanges(options => options.BatchSize = 1000);
{% endhighlight %}

---

### BatchTimeout
Gets or sets the maximum of time in seconds to wait for a batch before the command throws a timeout exception.

Read more: [BatchTimeout](batch-timeout)

{% include template-example.html %} 
{% highlight csharp %}
context.BulkSaveChanges(options => options.BatchTimeout = 180);
{% endhighlight %}

---

### BatchDelayInterval
Gets or sets a delay in milliseconds to wait between batch.

Read more: [BatchDelayInterval](batch-delay-interval)

{% include template-example.html %} 
{% highlight csharp %}
context.BulkInsert(list, options => options.BatchDelayInterval = 100);
{% endhighlight %}