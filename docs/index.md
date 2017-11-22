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
					<div class="display-4">Entity Framework Dynamic Filters</div>
				</h1>
				
				<div class="wow zoomIn">
					<a class="btn btn-xl btn-z wow zoomIn" role="button" href="https://www.nuget.org/packages/EntityFramework.DynamicFilters" target="_blank"
							onclick="ga('send', 'event', { eventAction: 'download-dynamic-filters'});">
						<i class="fa fa-cloud-download" aria-hidden="true"></i>
						NuGet Download
					<i class="fa fa-angle-right"></i>
					</a>
				</div>
				
				<div class="download-count">
					<div class="item-text">Download Count:</div>
					<div class="item-image wow lightSpeedIn"><img src="https://zzzprojects.github.io/images/nuget/entity-framework-dynamic-filters-big-d.svg" /></div>
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
							<div class="card-header">Extend Entity Framework</div>
							<div class="card-body">
{% highlight csharp %}
//Extend DbContext
context.DisableFilter("IsDeleted");
context.DisableAllFilters();
context.EnableAllFilters();

//Extend DbModelBuilder
modelBuilder.DisableFilterGlobally("IsDeleted");
modelBuilder.EnableFilterGlobally("IsDeleted");
modelBuilder.Filter("IsDeleted", 
	(ISoftDelete d) => d.IsDeleted, false);

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
{% highlight csharp %}	
//Specified as Func<object>
modelBuilder.Filter("Notes_CurrentUser", 
	(Note n) =>	n.PersonID, () => 
	GetPersonIDFromPrincipal(
	Thread.CurrentPrincipal));

// Specified as Func<DbContext, object>
modelBuilder.Filter("Notes_CurrentUser", 
	(Note n) => n.PersonID, (MyContext ctx) => 
	ctx.CurrentPersonID);
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
			<p class="mb-0">Thanks, Jonathan! And my words may be kind, but more importantly, they are true. Entity just doesn't work properly without Z!</p>
			<footer class="blockquote-footer">Robert J. McCarter, <a href="http://www.arapas.com/" target="_blank">Arapas Inc.</a></footer>
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
				
		<!-- more-feature !-->
		<div class="more">
			<a href="{{ site.github.url }}/tutorials" class="btn btn-z btn-xl" role="button">
				<i class="fa fa-book"></i>&nbsp;Read Tutorials
			</a>
		</div>
		
	</div>
</div>