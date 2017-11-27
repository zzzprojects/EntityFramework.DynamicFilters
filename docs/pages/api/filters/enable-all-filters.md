---
permalink: enable-all-filters
---

## Definition

EnableAllFilters method enables all filters within a DbContext at once which are globally disabled. 

{% include template-example.html%} 
{% highlight csharp %}

context.EnableAllFilters();

{% endhighlight %}

Enabling a globally disabled filters will apply only to that DbContext and it will not affect any other DbContext instances.

