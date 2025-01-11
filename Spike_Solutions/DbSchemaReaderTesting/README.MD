# DB Schema Reader Testing

This spike solution is to prove that the `DbSchemaReader` project can read the schema of a database and generate the necessary schema information for the Patchwork toolkit to generate the REST API endpoints.

It proves:

- We can connect to a remote database server
- We can read the schema of the database
- We can read the names of all the tables
- We can read the names of all the columns in each table
- We can read the primary key column of each table
- We can read the foreign key columns of each table
- We can read the data types of each column