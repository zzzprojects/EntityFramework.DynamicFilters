---
permalink: sql-compact
---

## SQL Server CE Support

SQL Server CE is supported with the following limitations:

 - The SQL Server CE provider does not support modifying the CommandText property during SQL interception. 
 - That is necessary in order to do some of the dynamic parameter value replacements. 
 - This means that Contains (IEnumerable) is not supported on SQL Server CE and will throw an exception.
 - SQL Server CE does not support the "like @value+'%'" syntax as mentioned [here](https://stackoverflow.com/questions/1916248/how-to-use-parameter-with-like-in-sql-server-compact-edition). 
 - So string.StartsWith(@value) is not supported and will throw a Format exception.
