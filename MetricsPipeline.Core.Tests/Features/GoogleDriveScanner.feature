Feature: Google Drive Scanner
  In order to enumerate directories via Google Drive
  As a developer using the core library
  I want GoogleDriveScanner to support listing child folders.

  Scenario: retrieving directories under a parent
    Given a drive folder contains two child folders
    When I request the list of Google drive directories
    Then both Google folder names should be returned
