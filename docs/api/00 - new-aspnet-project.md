The api project for MicCheck will contain the admin endpoints needed to manage features, environments, projects, settings and users.  It will also contain the endpoints for checking if a flag is enabled and returning current state of a flag.

## Requirements
1. Create a new asp.net project in /src/api/ called MicCheck.Api using .net 10 and asp.net webapi.
2. Ensure the project is configured to use OpenAPI and Swagger to document all endpoints
3. Create a GetToken endpoint that returns a JWT user token when requested
4. Create a corresponding unit test project in /tests/api called MicCheck.Api.Tests.Unit and use the latest version of NUnit and Moq as the testing frameworks
5. Create a .slnx solution file at the root of the project called MicCheck.slnx and create src and tests solution folders within it
6. Add the MicCheck API and Unit tests projects to the MicCheck.slnx 
7. Ensure the solution builds and all tests pass.

## Acceptance Criteria

- A new asp.net project exists at /src/api
- A corresponding unit test project exists with any code covered by unit test cases
- A new solution file called MicCheck.slnx was create and builds successfully
- Unit tests run without error
