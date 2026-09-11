For the `IInputProvider` classes allow the xml to define a mock CSV data set (could be a reference to a CSV filepath or could be inline) where the integration runner will use that mock data instead of actually calling the IO provider. A setting for the CLI would pass that "user mock data" for the integration runner to use. The setting could be "ALL" to use mock data for all or a comma spearated string to define the IO steps ids to use a mock for


--------------------

UseMockDataForSteps also needs to apply to IO Output Providers, but for those the mock mode is to just not connect or write to the file bu silenty ignore the input rows.

--------------------

Create an additional Input and Output provider that follows the same pattern as SQL and CSV but instead:

HttpInputProvider: calls an API expecting JSON and the allows configuring the json path to use as the input collection. Then it yields a record for each object in that collection. 

HttpOutputProvider: calls an API with payload being the JSON representation of the input row. Allow a configuration option in the provider to send the input row as xml or csv instead.

Have class that has common settings for both HTTP providers, with options for:
- Base URL
- HTTP Client Name (for .net dependency injection)
- optional OAuth2 client id and secret, scope and Token URL and HTTP Client Name (for .net dependency injection) for when calling the token endpoint
- optional static HTTP headers to populate in each request
- optional Client certificate thumbprint to be loaded from the windows certificate storage

Allow the providers to point to the base HTTP configuration object