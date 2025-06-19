Feature: Graph Scanner
  In order to enumerate directories via Microsoft Graph
  As a developer using the core library
  I want GraphScanner to support listing child folders and counting files.

  Scenario: retrieving directories under a parent
    Given a drive contains two child folders
    When I request the list of directories
    Then both folder names should be returned
