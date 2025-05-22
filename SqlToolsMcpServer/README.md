# SQL MCP Server Demo

This ASP.NET Core project demonstrates a simple Model Context Protocol (MCP) server that exposes SQL Server diagnostic tools via HTTP/SSE. It includes endpoints for retrieving missing-index recommendations and currently blocked queries.

## Prerequisites

* [.NET 7 SDK](https://dotnet.microsoft.com/download)
* SQL Server instance with access to system DMVs
* `ModelContextProtocol` C# SDK (installed via NuGet)

## Setup

1. **Clone the repository**

   ```bash
   git clone https://your-repo-url/SqlMcpServer.git
   cd SqlMcpServer
   ```

2. **Configure the connection string** in `appsettings.json`:

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=.;Database=YourDb;Trusted_Connection=True;"
     }
   }
   ```

3. **Restore and build**

   ```bash
   dotnet restore
   dotnet build
   ```

## Running the Server

Launch the MCP server:

```bash
dotnet run --project SqlMcpServer
```

By default, it will start on `http://localhost:5000`.

## Usage

You can interact with the MCP tools using any MCP-compatible client or simple HTTP requests.

### List Available Tools

```bash
curl http://localhost:5000/message --header "Content-Type: application/json" \
     --data '{"jsonrpc":"2.0","method":"mcp.listTools","id":1}'
```

### Invoke a Tool

* **GetMissingIndexes**

  ```bash
  curl http://localhost:5000/message --header "Content-Type: application/json" \
       --data '{
         "jsonrpc": "2.0",
         "method": "SqlTools.GetMissingIndexes",
         "params": [],
         "id": 2
       }'
  ```

* **GetBlockedQueries**

  ```bash
  curl http://localhost:5000/message --header "Content-Type: application/json" \
       --data '{
         "jsonrpc": "2.0",
         "method": "SqlTools.GetBlockedQueries",
         "params": [],
         "id": 3
       }'
  ```

#### Sample Response

```json
{
  "jsonrpc": "2.0",
  "result": [
    {
      "DatabaseName": "YourDb",
      "TableName": "Users",
      "EqualityColumns": "[LastName]",
      "InequalityColumns": null,
      "IndexAdvantage": 12345.67
    }
  ],
  "id": 2
}


