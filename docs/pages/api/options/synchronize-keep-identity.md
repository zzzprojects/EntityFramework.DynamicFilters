---
permalink: synchronize-keep-identity
---

## Definition
Gets or sets if the source identity value should be preserved on `Synchronize`. When not specified, identity values are assigned by the destination.

{% include template-example.html %} 
{% highlight csharp %}
context.BulkSynchronize(options => options.SynchronizeKeepIdentity = true);
{% endhighlight %}

## Purpose
The `SynchronizeKeepIdentity` option let you keep the source identity value when `synchronizing`.

By example, when importing a file, you may want to keep the value specified.