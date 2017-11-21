---
permalink: allow-duplicate-keys
---

## Definition
Gets or sets if a duplicate key is possible in the source.

{% include template-example.html %} 
{% highlight csharp %}
context.BulkMerge(list, options => options.AllowDuplicateKeys = true);
{% endhighlight %}

## Purpose
In a rare scenario such as importing a file, a key may be used in multiple rows.

In some provider such as SQL Server, the statement created by our library (`Merge`) make it impossible to use it with some duplicate keys.

By enabling this option, only the latest key is used instead.

