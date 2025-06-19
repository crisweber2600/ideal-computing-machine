Feature: Directory Counts Comparer
  In order to analyse differences between scanned maps
  As a developer
  I want the comparer to return only paths with mismatched counts.

  Scenario: identifying mismatched entries
    Given two maps with counts
    When I compare the maps
    Then only differing paths should be returned
