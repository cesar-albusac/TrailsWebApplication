# TrailsWebApplication

[![Build Status](https://dev.azure.com/cesypozo2/Hiking%20Trails/_apis/build/status%2Fcesar2.TrailsWebApplication?branchName=master)](https://dev.azure.com/cesypozo2/Hiking%20Trails/_build/latest?definitionId=11&branchName=master)

# Hiking Trails 🏞️

This project is a web application built using **ASP.NET Core MVC (Model View Controller) framework**. The primary goal of this project is to learn and utilize various **Azure** services. This project is dependent of [Trails Web API](https://github.com/cesar2/TrailsAPI "Trails ASP .NET Core Web API")

## Azure Services Used
The web application leverages the following Azure services:

* **App Service**: Used for hosting the web application and the API.

* **Blob Storage**: Used for storing the .gpx, images and thumbnail files.

* **Cosmos DB**: Used as the database for storing information about the hiking routes, such as Name, Distance, Duration, Difficulty, GPX and Image URLs.

* **Key Vault**: Used for securely storing and accessing secrets like connection strings, API keys, etc.

* **Azure Authentication**: Used for authenticating users to ensure secure access to the application.

* **Functions**: Used for running serverless functions to handle certain operations in the application, such as resizing the cover photo of a hiking route.

* Azure DevOps - **Pipelines**: Two pipelines were created to build and run tests within the API and the Web projects.

* Azure DevOps - **Artifacts**: As part of the API pipeline, a Nuget package is created that is being used in the Web Application.

Here is a basic diagram of how services are connected:

![Azure Diagram](https://github.com/cesar2/TrailsWebApplication/blob/master/Diagram.png)

## Application Features
The web application is designed to display hiking routes. It provides the following features:

* **CRUD Operations**: Users can create, read, update, and delete hiking routes.

* **Route Mapping**: Users can view a map displaying the hiking route along with its details.

This project serves as a great learning resource for understanding how to integrate and use Azure services in an ASP.NET Core MVC application.
