Feature: Directory Comparer
  In order to validate local copies
  As a developer
  I want missing or mismatched files reported.

  Scenario: comparing two folders
    Given the source directory contains "a.txt" with 10 bytes
    And the source directory contains "b.txt" with 5 bytes
    And the destination directory contains "b.txt" with 7 bytes
    When I compare the source and destination directories
    Then two mismatches should be reported
