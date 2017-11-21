---
layout: default
permalink: index
---

<!-- hero !-->
<div class="layout-angle">
	<div class="top-triangle wow slideInRight" data-wow-duration="1.5s"></div>
	<div class="layout-angle-inner">
<div class="hero">
	<div class="container">
		<div class="row">
		
			<div class="col-lg-5 hero-header">
			
				<h1>
					<div class="display-1">EFE</div>
					<div class="display-4">Entity Framework Extensions</div>
				</h1>
				
				<div class="wow zoomIn">
					<a class="btn btn-xl btn-z" href="{{ site.github.url }}/download"
							onclick="ga('send', 'event', { eventAction: 'download'});">
						<i class="fa fa-cloud-download" aria-hidden="true"></i>
						NuGet Download
						<i class="fa fa-angle-right"></i>
					</a>
				</div>
				
				<div class="download-count">
					<div class="item-text">Download Count:</div>
					<div class="item-image wow lightSpeedIn"><img src="https://zzzprojects.github.io/images/nuget/entity-framework-extensions-big-d.svg" /></div>
				</div>

				
			</div>
			
			<div class="col-lg-7 hero-examples">
			
				<div class="row hero-examples-1">
				
				
					<div class="col-lg-3 wow slideInUp"> 
						<h5 class="wow rollIn">EASY TO<br />USE</h5>
						<div class="hero-arrow hero-arrow-ltr">
							<img src="images/arrow-down1.png">
						</div>
					</div>

					<div class="col-lg-9 wow slideInRight">
						<div class="card card-code card-code-dark-inverse">
							<div class="card-header">Extend Entity Framework DbContext</div>
							<div class="card-body">
{% highlight csharp %}
// Bulk Operations
context.BulkSaveChanges();
context.BulkInsert(list);
context.BulkUpdate(list);
context.BulkDelete(list);
context.BulkMerge(list);

// Batch Operations
context.Customers.Where(x => !x.IsActif)
       .DeleteFromQuery();
context.Customers.Where(x => !x.IsActif)
       .UpdateFromQuery(x => 
            new Customer {IsActif = true});
{% endhighlight %}
							</div>
						</div>
					</div>
				</div>
				
				<div class="row hero-examples-2">
				
					<div class="col-lg-3 order-lg-2 wow slideInDown">
						<h5 class="wow rollIn">EASY TO<br />CUSTOMIZE</h5>
						<div class="hero-arrow hero-arrow-rtl">
							<img src="images/arrow-down1.png">
						</div>
					</div>
					
					<div class="col-lg-9 order-lg-1 wow slideInLeft">
						<div class="card card-code card-code-dark-inverse">
							<div class="card-header">Flexible and feature-rich API</div>
							<div class="card-body">
{% highlight csharp %}// Allow custom key	
context.BulkMerge(customers, options => {
   options.ColumnPrimaryKeyExpression = 
        customer => customer.Code;
});

// Allow child entities
context.BulkMerge(customers, 
	options => options.IncludeGraph = true);
});
{% endhighlight %}
							</div>
						</div>
					</div>						
				</div>
				
			</div>
			
		</div>
	</div>	
</div>
	</div>
	<div class="bottom-triangle-outer">
		<div class="bottom-triangle wow slideInLeft" data-wow-duration="1.5s"></div>
	</div>
</div>
<style>
.hero {
	background: transparent;
}
</style>

<!-- featured !-->
<div class="featured">
	<div class="container">
	
		<!-- Improve Performance !-->
		<h2 class="wow slideInUp">Improve SaveChanges <span class="text-z">Performance</span></h2>
		<div class="row">
			<div class="col-lg-5 left wow slideInLeft">
				<p>
					Make your save operations <span class="text-z">10 to 50 times</span> faster.
				</p>
				<p>
					Support all major providers:
				</p>
				
				<ul class="featured-list-sm">
					<li><i class="fa fa-check-square-o"></i>&nbsp;SQL Server 2008+</li>
					<li><i class="fa fa-check-square-o"></i>&nbsp;SQL Azure</li>
					<li><i class="fa fa-check-square-o"></i>&nbsp;SQL Compact</li>
					<li><i class="fa fa-check-square-o"></i>&nbsp;MySQL</li>					
					<li><i class="fa fa-check-square-o"></i>&nbsp;Oracle</li>
					<li><i class="fa fa-check-square-o"></i>&nbsp;PostgreSQL</li>
					<li><i class="fa fa-check-square-o"></i>&nbsp;SQLite</li>					
				</ul>	
			</div>
			<div class="col-lg-7 right wow slideInRight">
				<table>
					<thead>
						<tr>
							<th>Operations</th>
							<th>1,000 Entities</th>
							<th>2,000 Entities</th>
							<th>5,000 Entities</th>
						</tr>
					</thead>
					<tbody>
						<tr>
							<th>SaveChanges</th>
							<td>1,000 ms</td>
							<td>2,000 ms</td>
							<td>5,000 ms</td>
						</tr>
						<tr>
							<th>BulkSaveChanges</th>
							<td>90 ms</td>
							<td>150 ms</td>
							<td>350 ms</td>
						</tr>
						<tr>
							<th>BulkInsert</th>
							<td>6 ms</td>
							<td>10 ms</td>
							<td>15 ms</td>
						</tr>
						<tr>
							<th>BulkUpdate</th>
							<td>50 ms</td>
							<td>55 ms</td>
							<td>65 ms</td>
						</tr>
						<tr>
							<th>BulkDelete</th>
							<td>45 ms</td>
							<td>50 ms</td>
							<td>60 ms</td>
						</tr>
						<tr>
							<th>BulkMerge</th>
							<td>65 ms</td>
							<td>80 ms</td>
							<td>110 ms</td>
						</tr>
					</tbody>
				</table>

				<p class="text-muted">* Benchmark for SQL Server</p>
			</div>
		</div>
	</div>
</div>

<div class="testimonials">
{% include layout-angle-begin.html %}
	<div class="container">
		<h2>Amazing <span class="text-z">performance</span>, outstanding <span class="text-z">support</span>!</h2>
		
		<blockquote class="blockquote text-center wow slideInLeft">
			<p class="mb-0">We were very, very pleased with the customer support. There was no question, problem or wish that was not answered AND solved within days! We think that’s very unique!</p>
			<footer class="blockquote-footer">Klemens Stelzmüller, <a href="http://www.beka-software.at/" target="_blank">Beka-software</a></footer>
		</blockquote>
		<blockquote class="blockquote text-center wow slideInRight">
			<p class="mb-0">I’d definitely recommend it as it is a great product with a great performance and reliability.</p>
			<footer class="blockquote-footer">Eric Rey, <a href="http://www.transturcarrental.com/" target="_blank">Transtur</a></footer>
		</blockquote>
		<blockquote class="blockquote text-center wow slideInLeft">
			<p class="mb-0">It’s great. It took me 5 minutes to implement it and makes my application 100x more responsive for certain database operations.</p>
			<footer class="blockquote-footer">Dave Weisberg</footer>
		</blockquote>

		<div class="more">
			<a href="http://www.zzzprojects.com/testimonials/" target="_blank" class="btn btn-lg btn-z" role="button"
					onclick="ga('send', 'event', { eventAction: 'testimonials'});">
				<i class="fa fa-comments"></i>&nbsp;
				Read More Testimonials
			</a>
		</div>
	</div>
{% include layout-angle-end.html %}
</div>


<!-- features !-->
<div class="features">

	<div class="container">
		
		<!-- Bulk SaveChanges !-->
		<h2 class="wow slideInUp">Bulk SaveChanges</h2>
		<div class="row">
			<div class="col-lg-5 wow slideInLeft">
				<p class="feature-tagline">Add 4 letters <span class="text-z">Bulk</span> to make your application <span class="text-z">10-50 times</span> faster and more responsive.</p>
				<ul>
					<li>Easy to use</li>
					<li>Easy to customize</li>
					<li>Easy to maintain</li>
				</ul>
				<div class="more-info">
					<a href="{{ site.github.url }}/tutorial-bulk-savechanges" class="btn btn-lg btn-z" role="button">
						<i class="fa fa-book"></i>&nbsp;
						Read More
					</a>
				</div>	
			</div>
			<div class="col-lg-7 wow slideInRight">
				<div class="card card-code card-code-light">
					<div class="card-header">Bulk SaveChanges Examples</div>
					<div class="card-body">
{% highlight csharp %}
// Easy to use
context.BulkSaveChanges();

// Easy to customize
context.BulkSaveChanges(options => options.BatchSize = 1000);
{% endhighlight %}
					</div>
				</div>
			</div>
		</div>

		<hr class="m-y-md" />
		
		<!-- Bulk Operations !-->
		<h2 class="wow slideInUp">Bulk Operations</h2>
		<div class="row">
			<div class="col-lg-5 wow slideInLeft">
				<p class="feature-tagline">Add the maximum <span class="text-z">flexibility</span> to cover every scenario.</p>
				<ul>
					<li>Bulk Insert</li>
					<li>Bulk Update</li>
					<li>Bulk Delete</li>
					<li>Bulk Merge</li>
					<li>Bulk Synchronize</li>
				</ul>
				<div class="more-info">
					<a href="{{ site.github.url }}/tutorial-bulk-operations" class="btn btn-lg btn-z" role="button">
						<i class="fa fa-book"></i>&nbsp;
						Read More
					</a>
				</div>	
			</div>
			<div class="col-lg-7 wow slideInRight">
				<div class="card card-code card-code-light">
					<div class="card-header">Bulk Operations Examples</div>
					<div class="card-body">
{% highlight csharp %}

// Allow custom key	
context.BulkMerge(customers, options => {
   options.ColumnPrimaryKeyExpression = 
        customer => customer.Code;
});

// Allow child entities
context.BulkMerge(customers, 
	options => options.IncludeGraph = true);
});
{% endhighlight %}	
					</div>
				</div>
			</div>
		</div>
		
		<hr class="m-y-md" />
		
		<!-- Batch Operations !-->
		<h2 class="wow slideInUp">Batch Operations</h2>
		<div class="row">
			<div class="col-lg-5 wow slideInLeft">
				<p class="feature-tagline">Perform bulk operations from LINQ Query without loading entities in the context.</p>
				<ul>
					<li>DeleteFromQuery</li>
					<li>UpdateFromQuery</li>
				</ul>
				<div class="more-info">
					<a href="{{ site.github.url }}/tutorial-batch-operations" class="btn btn-lg btn-z" role="button">
						<i class="fa fa-book"></i>&nbsp;
						Read More
					</a>
				</div>	
			</div>
			<div class="col-lg-7 wow slideInRight">
				<div class="card card-code card-code-light">
					<div class="card-header">Batch Operations Examples</div>
					<div class="card-body">
{% highlight csharp %}
// DELETE all inactive customers 
context.Customers.Where(x => !x.IsActif)
       .DeleteFromQuery();
	   
// UPDATE all inactive customers
context.Customers.Where(x => !x.IsActif)
       .UpdateFromQuery(x => 
            new Customer {IsActif = true});
{% endhighlight %}	
					</div>
				</div>
			</div>
		</div>
		
		<!-- more-feature !-->
		<div class="more">
			<a href="{{ site.github.url }}/tutorials" class="btn btn-z btn-xl" role="button">
				<i class="fa fa-book"></i>&nbsp;Read Tutorials
			</a>
		</div>
		
	</div>
</div>