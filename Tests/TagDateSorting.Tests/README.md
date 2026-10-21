# Tag assignment date sorting tests

Run `dotnet test Tests/TagDateSorting.Tests/TagDateSorting.Tests.csproj` from the repository root.

The suite exercises relational ordering with SQLite, SQL translation for the production MySQL entities, API validation, and Blazor selection and persistence behavior.

For execution against MySQL, set `SPOTIFY_TEST_MYSQL_CONNECTION` to a **local test server** connection string. The account must be able to create and drop databases. The test creates a uniquely named `codex_tagdate_*` database and drops it afterward; it does not use the database named in the connection string. Without this variable, only that test is skipped.
