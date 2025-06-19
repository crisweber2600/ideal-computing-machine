Feature: Directory Scanner
  In order to aggregate counts for all subfolders
  As a developer
  I want DirectoryScanner to record counts for each nested directory.

  Scenario: scanning nested directories
    Given a drive root with nested folders
    When the directory scanner processes the root
    Then counts for every directory should be stored
