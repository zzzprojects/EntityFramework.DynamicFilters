---
permalink: set-filter-scoped-parameter-value
---

SetFilterScopedParameterValue sets the parameter for a filter within the current DbContext scope.  Once the DbContext is disposed, this parameter will no longer be in scope and will be removed.

{% include template-example.html%} 
{% highlight csharp %}

//Change Soft Delete filter to return only deleted records
context.SetFilterScopedParameterValue("IsDeleted", true);

//For multiple parameters, you must specify the name of the parameter
context.SetFilterScopedParameterValue("BlogEntryFilter", "accountID", 12345);

{% endhighlight %}

Parameter values can be set to a specific value as showb in the above example, but it can also be set to a delegate expressions (**Func < object >** or **Func<DbContext, object>**).

{% include template-example.html%} 
{% highlight csharp %}

context.SetFilterScopedParameterValue("EntityCFilter", "status", (TestContext ctx) => ctx.Status);

{% endhighlight %}
