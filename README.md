# Store Traffic Data Integration Program

This program integrates store traffic data from the ShopperTrak API into an Oracle database. It fetches real-time traffic data, validates the database schema, and inserts the data into the database.

## Table of Contents
- [Requirements](#requirements)
- [Setup](#setup)
- [Usage](#usage)
- [Code Overview](#code-overview)
- [Error Handling](#error-handling)
- [License](#license)

## Requirements
- Visual Studio
- .NET Core SDK
- Oracle OLE DB Provider
- HttpClient for .NET
- Oracle Database

## Setup

1. **Install Visual Studio**: Download and install Visual Studio from [Microsoft's website](https://visualstudio.microsoft.com/downloads/).

2. **Oracle OLE DB Provider**: Ensure that the Oracle OLE DB Provider is installed and properly configured.

3. **Clone the Repository**: Clone this repository to your local machine.
    ```sh
    git clone https://github.com/fired/shoppertrak-to-gers.git
    ```

4. **Open the Project in Visual Studio**:
    - Open Visual Studio.
    - Go to `File` > `Open` > `Project/Solution`.
    - Select the `ShopperTrakAPI.sln` file.

5. **Configure Database Connection**: Update the `connectionString` variable in the `Main` method with your Oracle database connection details.
    ```csharp
    string connectionString = "YOUR_CONNECTION_STRING_HERE";
    ```

6. **Set API Credentials**: Update the `username` and `password` variables in the `Main` method with your ShopperTrak API credentials.
    ```csharp
    string username = "YOUR_API_USERNAME";
    string password = "YOUR_API_PASSWORD";
    ```

## Usage

1. **Build the Project**:
    - Go to `Build` > `Build Solution` or press `Ctrl+Shift+B`.

2. **Run the Project**:
    - Go to `Debug` > `Start Without Debugging` or press `Ctrl+F5`.

## Code Overview

### Store Mapping
A dictionary `storeMapping` is used to map store IDs to their respective names and locations.

### Main Method
The `Main` method orchestrates the program's flow:
1. Generates the API URL.
2. Fetches data from the API.
3. Validates the database table structure.
4. Inserts the data into the database.

### GenerateApiUrl Method
Generates the API URL with the current date and time, rounded to the nearest 15-minute increment.

### GetApiData Method
Fetches data from the ShopperTrak API using the provided URL and credentials.

### ValidateTableStructure Method
Validates the structure of the `STORETRAFFIC` table in the Oracle database.

### InsertDataIntoDatabase Method
Parses the XML data from the API and inserts it into the `STORETRAFFIC` table.

## Error Handling
- **Exception Handling**: The program catches and logs exceptions during API data fetching and database operations.
- **Validation**: Ensures the database table structure matches the expected schema.

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---

Feel free to contribute to this project by submitting issues or pull requests on the [GitHub repository](https://github.com/fired/shoppertrak-to-gers.git).
