<p align="center">
  <b>English</b> | <a href="README.el.md">Ελληνικά</a>
</p>

---

![UniNotes Logo](https://i.imgur.com/jMGS9uL.gif)

## Project Description

UniNotes is an online platform for sharing and managing notes for university students. It allows users to upload, search, and share academic notes organized by semester and course.

## Technologies

  - .NET 9.0
  - Blazor
  - MongoDB
  - HTML/CSS/JavaScript

## Installation and Execution Instructions

### Prerequisites

  - .NET 9.0 SDK
  - MongoDB (locally or on a cloud service)

### Installation Steps

1.  **Clone the repository**

    ```
    git clone https://github.com/yourusername/UniNotes.git
    cd UniNotes
    ```

2.  **Setup MongoDB**

      - Create an account on MongoDB Atlas or install MongoDB locally
      - Create a new database for UniNotes

3.  **Configure AppSettings**

      - Create an `appsettings.json` file or rename `appsettings.template.json` to `appsettings.json` in the main project folder (UniNotes/)
      - Use the template below, replacing `<CONNECTION_STRING>` with your MongoDB connection string and `<WEBHOOK_URL>` with your Discord Webhook URLs:

    <!-- end list -->

    ```json
     {
     "ConnectionStrings": {
         "MongoDb": "<CONNECTION_STRING>"
     },
     "Logging": {
         "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
         }
     },
     "AllowedHosts": "*",
     "DiscordWebhook": {
         "ReportUrl": "<WEBHOOK_URL>",
         "SecretaryUrl": "<WEBHOOK_URL>"
         }
     }
    ```

    **Note**: Some versions of Visual Studio create this file automatically, but for users outside of Windows or on other IDEs, it is necessary to create it manually. This file is not included in the repository for security reasons.

4.  **Install Dependencies and Run**

    ```
    dotnet restore
    dotnet run
    ```

5.  **Access the Application**

      - Open your browser and navigate to `http://localhost:5073` or `https://localhost:7073`

## Application Structure and Pages

### Main Pages

  - **Login.razor**: User login page
  - **Register.razor**: New user registration page
  - **Dashboard.razor**: Main user dashboard after login
  - **Notes.razor**: View and search notes from all users
  - **MyNotes.razor**: Manage the user's personal notes
  - **Upload.razor**: Page for uploading new notes
  - **Settings.razor**: User profile settings
  - **Credits.razor**: Information about the creators

## Functionality

#### User Management

  - User registration and login
  - User profile and settings management
  - Different access levels (regular users, moderators, secretariat)

#### Note Management

  - Uploading notes in various formats (PDF, Word, images)
  - Categorization by semester and course
  - Search and filtering of notes
  - Viewing notes with an embedded viewer

#### User Communication

  - Rating notes (future feature)
  - Reporting problematic notes (future feature)
  - Saving favorite notes (future feature)

## Todo List

### 1\) QOL and necessary updates:

  - [x] 1.1) If you go to /dashboard and refresh, you should stay logged in (whichever page you go to, if you refresh, it should throw you to the dashboard or landing)
  - [x] 1.2) We need to change the way we switch from page to page
  - [x] 1.3) On every page, there should be an option to go to all other pages
  - [x] 1.4) Back function on register and login
  - [x] 1.5) Translate the entire site to Greek
  - [x] 1.6) Create a README.MD
  - [x] 1.7) Make the view file work on every note
  - [x] 1.8) Make the "Notes Forum" scrollable for mobile devices

### 2\) Remaining user functions:

  - [x] 2.1) Add ratings to notes
  - [x] 2.2) Add a report bug function
  - [x] 2.3) Add a report note function
  - [x] 2.4) Add favorites for notes
  - [x] 2.5) Add view profile for each user and average rating from their notes
  - [x] 2.6) Add tags to notes
  - [x] 2.7) Add download function for notes

### 3\) Create moderator role, which has the same features as users and:

  - [x] 3.1) Check notes
  - [x] 3.3) Check courses
  - [x] 3.4) Update courses

### 4\) We need to add the secretariat

  - [x] 4.1) Implementation of secretariat functions
  - [x] 4.2) User and permission management

## Development Challenge

UniNotes is being developed to offer a comprehensive solution for students to share and access quality notes. The main development challenges include:

1.  Ensuring data security and intellectual property rights
2.  Optimizing loading times for large files
3.  Implementing a user-friendly interface
4.  Effective management of the MongoDB database
5.  Implementation of different access levels and permissions

## Screenshots
<img width="1919" height="1079" alt="image" src="https://github.com/user-attachments/assets/010df0ac-e612-4c33-a1f0-6dc789c5f589" />
<img width="1919" height="1079" alt="image2" src="https://github.com/user-attachments/assets/428e409c-be3e-4abf-be11-5899c82cc48c" />
<img width="1919" height="1079" alt="image3" src="https://github.com/user-attachments/assets/d3029730-460c-47b8-a72a-3dde85a43541" />
<img width="1919" height="1079" alt="image4" src="https://github.com/user-attachments/assets/b8d08d66-35ff-42be-aa50-3465fe74ed67" />

## Contribution

If you wish to contribute to the project, please follow the steps below:

1.  Fork the repository
2.  Create a new branch for the feature (`git checkout -b feature/amazing-feature`)
3.  Set up the development environment according to the installation instructions above
4.  Commit your changes (`git commit -m 'Add new feature'`)
5.  Push to the branch (`git push origin feature/amazing-feature`)
6.  Open a Pull Request

## Contact

### anastasios.tsalkos@gmail.com

### christos.daglaroglou@gmail.com

### panoschatzikallias@gmail.com

-----

Created with ❤️ by students for students
