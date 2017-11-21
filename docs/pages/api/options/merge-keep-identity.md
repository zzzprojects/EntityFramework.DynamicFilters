---
permalink: merge-keep-identity
---

## Definition
Gets or sets if the source identity value should be preserved on `Merge`. When not specified, identity values are assigned by the destination.

{% include template-example.html %} 
{% highlight csharp %}
context.BulkMerge(options => options.MergeKeepIdentity = true);
{% endhighlight %}

## Purpose
The `MergeKeepIdentity` option let you keep the source identity value when `merging`.

By example, when importing a file, you may want to keep the value specified.