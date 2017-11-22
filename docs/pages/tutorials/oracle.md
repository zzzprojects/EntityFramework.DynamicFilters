---
permalink: oracle
---

## Oracle Support

Oracle is supported using the [Official Oracle ODP.NET, Managed Entity Framework Driver](https://www.nuget.org/packages/Oracle.ManagedDataAccess.EntityFramework) with the following limitations:

 - The Oracle driver does not support generating an "in" expression. 
 - Using the "Contains" operator will result in outputting a series of equals/or expressions.
 - Using a DateTime value tends to throw an exception saying ***"The member with identity 'Precision' does not exist in the metadata collection"***.
 - This seems to be a bug in the Oracle driver. 
 - Using a DateTimeOffset instead of a DateTime works correctly (which also then uses the Oracle TIMESTAMP datatype instead of DATE).