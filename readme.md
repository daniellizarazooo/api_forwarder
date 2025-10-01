# API Forwarder

## Project Overview

This project is a .NET Core API that acts as a forwarder for requests to other APIs. It is designed to be used as a central point of access for multiple devices, allowing for a single point of control and monitoring. The application is built with a focus on performance and scalability, using a background service to fetch data from the APIs and storing the data in memory for fast access.

## Features

-   **API Forwarding**: Forward requests to other APIs.
-   **Data Caching**: Store data in memory for fast access.
-   **Background Service**: Fetch data from APIs in the background.
-   **Swagger UI**: Interactive API documentation.
-   **Serilog Logging**: Log errors to a file.
-   **CORS Support**: Allow requests from any origin.

## API Endpoints

The following endpoints are available:

-   `GET /proxy/light/all`: Get all light intensities.
-   `GET /proxy/scene/all`: Get all scenes.
-ax
-   `GET /proxy/light`: Get the light intensity for a given URL.
-   `GET /proxy/scene`: Get the scene for a given URL.
-   `POST /proxy/scene`: Set the scene for a given URL.

### `GET /proxy/light/all`

Get all light intensities.

**Response:**

```json
{
    "https://example.com/light": {
        "token": "your-token",
        "value": 100,
        "name": "Light 1"
    }
}
```

### `GET /proxy/scene/all`

Get all scenes.

**Query Parameters:**

-   `on` (boolean): Filter for scenes that are not 0.

**Response:**

```json
[
    {
        "name": "Scene 1",
        "value": 1
    }
]
```

### `GET /proxy/light`

Get the light intensity for a given URL.

**Query Parameters:**

-   `url` (string): The URL of the light.
-   `token` (string): The token for the light.
-   `name` (string): The name of the light.

**Response:**

```
100
```

### `GET /proxy/scene`

Get the scene for a given URL.

**Query Parameters:**

-   `url` (string): The URL of the scene.
-   `token` (string): The token for the scene.
-   `name` (string): The name of the scene.

**Response:**

```
1
```

### `POST /proxy/scene`

Set the scene for a given URL.

**Request Body:**

```json
{
    "url": "https://example.com/scene",
    "token": "your-token",
    "scene": 1
}
```

**Response:**

```
1
```

## Configuration

The configuration for the application is stored in the `appsettings.json` file. The following options are available:

-   `Logging`: Configure the logging level.
-   `AllowedHosts`: Configure the allowed hosts.
-   `Kestrel`: Configure the Kestrel server.

## Running the Application

To run the application locally, you can use the following command:

```bash
dotnet run
```

The application will be available at `http://localhost:5005`.

## Running as a Service

To run the application as a Windows service, you can use the following commands:

```bash
nssm install <service name> <path to exe>
nssm start <service name>
```

## Logging

The application uses Serilog for logging. Logs are stored in the `Logs` folder.

## Project Structure

The project has the following folder structure:

-   `Controllers`: Contains the API controllers.
-   `Models`: Contains the data models.
-   `Services`: Contains the background service.
-   `Store`: Contains the data store.
-   `Properties`: Contains the launch settings.
-   `Logs`: Contains the log files.
-   `publish`: Contains the published files.

## Building and Publishing

To build and publish the project, you can use the following command:

```bash
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -p:PublishTrimmed=false -o ./publish
```

This will create a single executable file in the `publish` folder that can be run as a service.

## Code Explanation

### `Program.cs`

This file is the entry point of the application. It is responsible for configuring the services, setting up the HTTP request pipeline, and running the application.

### `Controllers/LightingController.cs`

This file contains a controller for handling lighting-related requests. It is currently commented out and not used in the application.

### `Models/Models.cs`

This file contains the data models for the application. The following models are defined:

-   `Data`: Represents a data object with a token, a value, and a name.
-   `LightingResponse`: Represents a response from a lighting API.
-   `Link`: Represents a link.
-   `SceneResponse`: Represents a response from a scene API.
-   `SceneToSet`: Represents a scene to be set.
-   `ErrorLogger`: Logs errors to the console.

### `Services/ApiFetcher.cs`

This file contains a background service that fetches data from the APIs. The `RunAsync2` method is responsible for fetching the data and updating the data store.

### `Store/Store.cs`

This file contains the data store for the application. The `DictStore` class is a generic class that can be used to store any type of data. The `Intensities` and `Scenes` classes are concrete implementations of the `DictStore` class.