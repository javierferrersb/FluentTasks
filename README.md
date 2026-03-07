# FluentTasks

A Windows desktop app for managing your [Google Tasks](https://tasks.google.com), built with WinUI 3 and .NET 10.

## Features

- **Google Tasks sync** — OAuth2 sign-in with auto-sync
- **Full task management** — create, edit, complete, delete, and reorder tasks via drag & drop
- **Subtasks** — nest tasks under a parent task
- **Due dates & overdue detection** — visual indicators for tasks past their due date
- **Filtering** — All, Incomplete, Completed, Overdue, Today, This Week
- **Sorting** — by due date (asc/desc), alphabetically (asc/desc), or completed-last
- **Search** — full-text search across task titles and notes
- **Multiple task lists** — manage lists with custom icons
- **Settings** — default filter/sort, language preference
- **Localization** — 22+ languages, auto-detected from Windows display language
- **Keyboard shortcuts**

## Screenshots

<img width="1363" height="858" alt="Main Window" src="https://github.com/user-attachments/assets/ddd68404-b678-40fd-88f4-4bc0d87446d2" />

<img width="1363" height="858" alt="Task Details" src="https://github.com/user-attachments/assets/e59d9123-46ae-4aff-9f40-2eb1c5db3c8d" />

<img width="1363" height="858" alt="Keyboard Shortcuts" src="https://github.com/user-attachments/assets/652bc9a8-0e65-46ea-8310-7cf4943a35dd" />

<img width="1363" height="858" alt="Custom Lists Icons" src="https://github.com/user-attachments/assets/2b4dd839-21d5-4f29-9d75-ebba74d4ba00" />

## Requirements

- Windows 10 version 1809 (build 17763) or later
- A Google account with Google Tasks enabled
- A `client_secrets.json` file (Google OAuth credentials) placed next to the executable

  > See `client_secrets.json.template` for the expected format. Obtain credentials from the [Google Cloud Console](https://console.cloud.google.com/).

## Project Structure

| Project | Description |
|---|---|
| `FluentTasks.Core` | Domain models (`TaskItem`, `TaskList`) and service interfaces |
| `FluentTasks.Infrastructure` | Google Tasks & Auth API implementation |
| `FluentTasks` (UI) | WinUI 3 app — views, view models, dialogs, controls |

## Building

1. Install [Visual Studio 2022](https://visualstudio.microsoft.com/) with the **Windows App SDK** workload and **.NET 10 SDK**.
2. Clone the repository.
3. Add your `client_secrets.json` to the output directory (or configure it as a build asset).
4. Open `FluentTasks.sln` and build for `x64`, `x86`, or `ARM64`.

## Tech Stack

- [WinUI 3](https://learn.microsoft.com/windows/apps/winui/winui3/) via Windows App SDK 1.8
- [.NET 10](https://dotnet.microsoft.com/)
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) (MVVM, source generators)
- [Google APIs Client Library for .NET](https://github.com/googleapis/google-api-dotnet-client) (Tasks v1, Auth)
- `Microsoft.Extensions.Hosting` for dependency injection
