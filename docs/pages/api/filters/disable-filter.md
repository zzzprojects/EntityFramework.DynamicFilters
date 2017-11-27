---
permalink: disable-filter
---

## Definition

DisableFilter selectively disable the filter which is globally enabled. 

{% include template-example.html%} 
{% highlight csharp %}

context.DisableFilter("IsDeleted");

{% endhighlight %}

Disabling a globally enabled filter will apply only to that DbContext and it will not affect any other DbContext instances.

## Disable Filter Globally

DisableFilterGlobally disables the filter globally and it Can be enabled as needed via DbContext.EnableFilter().

{% include template-example.html%} 
{% highlight csharp %}

modelBuilder.DisableFilterGlobally("IsDeleted");

{% endhighlight %}