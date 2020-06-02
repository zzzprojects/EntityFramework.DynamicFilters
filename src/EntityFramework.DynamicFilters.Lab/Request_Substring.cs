using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;

namespace EntityFramework.DynamicFilters.Core.Lab
{
	class Request_Substring
	{
		public static void Execute()
		{   // ADD seed data here
			using (var context = new EntityContext())
			{
				context.Database.Delete();
				context.Database.Create();
				context.Customers.Add(new Customer() { Name = "Customer_A", IsActive = false, Code = "00001.00003.00004" });
				context.Customers.Add(new Customer() { Name = "Customer_B", IsActive = true, Code = "00002.00003.00004" });
				context.Customers.Add(new Customer() { Name = "Customer_C", IsActive = false, Code = "00003.00003.00004" });
				context.SaveChanges();

			}

			// ADD code to reproduce the issue here (Add filter in the context if required)
			using (var context = new EntityContext())
			{
				// issue
			}

			using (var context = new EntityContext())
			{ 
				var test = context.Customers.ToList();
			}
		}
		public class EntityContext : DbContext
		{
			public static string DataBaseName = "KillMe";
			// [REPLACE] is in Beta.
			public static string ConnectionString =
				("Server=[REPLACE];Initial Catalog = [BD]; Integrated Security = true; Connection Timeout = 35; Persist Security Info=True").Replace("[REPLACE]", Environment.MachineName).Replace("[BD]", DataBaseName);

			public EntityContext() : base(ConnectionString)
			{

			}

			public DbSet<Customer> Customers { get; set; }

			// ADD code to reproduce the issue here 
			protected override void OnModelCreating(DbModelBuilder modelBuilder)
			{
				// Example
				 
				 modelBuilder.Filter("CodeFilter", (Customer d,List<string> AllowedProjectCodes) =>
						 AllowedProjectCodes.Contains(d.Code.Substring(0, 5)) ,
									()=>LoadParentCodesFromPrincipal()					   
								   ); 

			}

			private List<string> LoadParentCodesFromPrincipal()
			{
				return new List<string>() { "00001", "00005" };
			}
		}


		public class Customer
		{
			public int CustomerID { get; set; }
			public string Name { get; set; }
			public Boolean IsActive { get; set; }
			public string Code { get; set; }
		}
	}
} 