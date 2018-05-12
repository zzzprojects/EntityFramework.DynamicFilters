# Active/Inactive

## Active/Inactive

Practically every database based application has to deal with "active/inactive" records. 

The records in a table are partitioned on the active flag, so that active records are in one partition, and inactive records are in the other partition.


```csharp

modelBuilder.Filter("IsActive", (BlogEntry b) => b.IsActive, true);

```

In this example, "**IsActive**" filter will automatically filter BlogEntry entities by applying **IsActive == true**.    