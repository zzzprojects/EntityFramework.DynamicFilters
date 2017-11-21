---
permalink: key
---

- [AllowDuplicateKeys](#allowduplicatekeys)
- [AllowUpdatePrimaryKeys](#allowupdateprimarykeys)

---

## AllowDuplicateKeys
Gets or sets if a duplicate key is possible in the source.

Read more: [AllowDuplicateKeys](allow-duplicate-keys)

{% include template-example.html %} 
{% highlight csharp %}
context.BulkSaveChanges(options => options.AllowDuplicateKeys = true);
{% endhighlight %}

---

## AllowUpdatePrimaryKeys
Gets or sets of the key must also be included in columns to `UPDATE`.

Read more: [AllowUpdatePrimaryKeys](allow-update-primary-keys)

{% include template-example.html %} 
{% highlight csharp %}
context.BulkSaveChanges(options => options.AllowUpdatePrimaryKeys = true);
{% endhighlight %}
