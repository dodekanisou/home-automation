# Home automation project

This is a hobby project that was created during Christmas 2019 in order to:
- Allow people in my building to controll public doors (including garage) from their mobile phones
- Play with the Auth0.com service
- Discover what's new in .net core 3.1

## Handling secrets

You should always treat your codebase as if it was public, thus no secrets should be stored in the repository. See [Microsoft docs reference](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-3.1).
In this project's case, we are using the secret manager approach. Right click on the project and select "Manage User Secrets" to specify your settings.

## Corners that were cut

Dependency injection and testability of this project was not in scope of the MVP.