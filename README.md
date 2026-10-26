# LocalFileDB

LocalFileDB is a Windows desktop application intended to provide a visual editor for a file catalog. The catalog is stored in a configuration file that acts as the database and contains information about files and directories.

The planned application will let people browse catalog entries, edit their metadata, and add or remove entries. The project is currently an initial WPF application scaffold; these capabilities are planned and may not yet be implemented.

## Technology

- C# and .NET 10
- WPF

## Architecture

The application follows the MVVM pattern. WPF views present and bind to ViewModels; ViewModels expose screen state and commands; models represent catalog data; and services handle catalog and configuration-file operations. Application behavior should stay out of view code-behind.

## Project

Open `LocalFileDB.slnx` in Visual Studio to work with the solution. The application project is in the `LocalFileDB` directory.

## For contributors

This README is written for people and should contain information useful to human readers. Agent-specific working instructions are maintained separately in `AGENTS.md`.
