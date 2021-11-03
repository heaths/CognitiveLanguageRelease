# Azure Cognitive Service - Language SDKs Release Sample

This is a simple project used to test signed .nupkgs before releasing.

1. Download the packages-signed.zip from the release artifacts.
1. Extract to a directory e.g., *~/downloads/packages-signed* where all package directories are directly therein e.g. *~/packages-signed/Azure.AI.Language.QuestionAnswering*.
1. Update package references:

   ```bash
   dotnet add package --prerelease --source "$HOME/downloads/packages-signed" Azure.AI.Language.Conversations
   dotnet add package --prerelease --source "$HOME/downloads/packages-signed" Azure.AI.Language.QuestionAnswering
   ```

1. Build and run the project:

   ```bash
   dotnet run
   ```

   Run `dotnet run -- --help` for usage.
