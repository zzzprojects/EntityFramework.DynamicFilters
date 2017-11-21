---
permalink: allow-update-primary-keys
---

## Definition
Gets or sets of the key must also be included in columns to `UPDATE`.

{% include template-example.html %} 
{% highlight csharp %}
context.BulkMerge(list, options => options.AllowUpdatePrimaryKeys = true);
{% endhighlight %}

## Purpose
This option is rarely used. One scenario example is a custom key with a trigger that requires columns part of the key to also be `UPDATED`.