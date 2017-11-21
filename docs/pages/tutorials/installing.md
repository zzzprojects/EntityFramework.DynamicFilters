---
permalink: installing
---

**Entity Framework Extensions** can be installed through NuGet.

This library is **NOT FREE**

The latest version always contains a trial that expires at the end of the month. You can extend your trial for several months by downloading the latest version at the start of every month.

## Step 1 - NuGet Download

Choose the Entity Framework version and the Package Manager you want to use to download **Entity Framework Extensions**.

<div class="row">
	<div class="col-lg-6">
		<div class="card card-layout-z2 wow slideInLeft">
			<div class="card-header wow slideInDown">
				<h3>Entity Framework 6 (EF6)</h3>
			</div>
			<div class="card-body wow slideInUp">
				<a class="btn btn-lg btn-z" role="button" href="https://www.nuget.org/packages/Z.EntityFramework.Extensions/" onclick="ga('send', 'event', { eventAction: 'download'});" style="visibility: visible; animation-name: pulse;">
					<i class="fa fa-cloud-download" aria-hidden="true"></i>
					NuGet Download
				</a>
				<div>Download Count:</div>
				<div class="download-count2"><img src="https://zzzprojects.github.io/images/nuget/entity-framework-extensions-big-d.svg"></div>
			</div>
		</div>
	</div>
	<div class="col-lg-6">
		<div class="card card-layout-z2 wow slideInRight">
			<div class="card-header wow slideInDown">
				<h3>Entity Framework Core (EF Core)</h3>
			</div>
			<div class="card-body wow slideInUp">
				<a class="btn btn-lg btn-z" role="button" href="https://www.nuget.org/packages/Z.EntityFramework.Extensions.EFCore/" onclick="ga('send', 'event', { eventAction: 'download'});" style="visibility: visible; animation-name: pulse;">
					<i class="fa fa-cloud-download" aria-hidden="true"></i>
					NuGet Download							
				</a>
				<div>Download Count:</div>
				<div class="download-count2"><img src="https://zzzprojects.github.io/images/nuget/entity-framework-extensions-efcore-big-d.svg"></div>
			</div>
		</div>
	</div>
</div>
<br /><br /><br />
<div class="row">
	<div class="col-lg-6">
		<div class="card card-layout-z2 wow slideInLeft">
			<div class="card-header wow slideInDown">
				<h3>Entity Framework 5 (EF5)</h3>
			</div>
			<div class="card-body wow slideInUp">
				<a class="btn btn-lg btn-z" role="button" href="https://www.nuget.org/packages/Z.EntityFramework.Extensions.EF5/" onclick="ga('send', 'event', { eventAction: 'download'});" style="visibility: visible; animation-name: pulse;">
					<i class="fa fa-cloud-download" aria-hidden="true"></i>
					NuGet Download							
				</a>
				<div>Download Count:</div>
				<div class="download-count2"><img src="https://zzzprojects.github.io/images/nuget/entity-framework-extensions-ef5-big-d.svg"></div>
			</div>
		</div>
	</div>
</div>

## Step 2 - Done

**Entity Framework Extensions** doesn't require any configuration by default.

All bulk operations extension methods are automatically added to your DbContext:
- BulkSaveChanges
- BulkInsert
- BulkUpdate
- BulkDelete
- BulkMerge
- BulkSynchronize

{% include template-example.html title='Bulk Operations Examples'%} 
{% highlight csharp %}
// BulkSaveChanges
context.BulkSaveChanges();

// Bulk Operations
context.BulkInsert(list);
context.BulkUpdate(list);
context.BulkDelete(list);
context.BulkMerge(list);
context.BulkSynchronize(list);
{% endhighlight %}

All batch operations extension methods are automatically added to your Queryable:
- DeleteFromQuery
- UpdateFromQuery

{% include template-example.html title='Batch Operations Examples'%} 
{% highlight csharp %}
// Batch Operations
context.Customers.Where(x => !x.IsActif).DeleteFromQuery();
context.Customers.Where(x => !x.IsActif).UpdateFromQuery(x => new Customer {Actif = true});
{% endhighlight %}
