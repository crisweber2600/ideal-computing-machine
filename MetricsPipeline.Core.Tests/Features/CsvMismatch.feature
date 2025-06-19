Feature: CSV Export
  In order to review drive differences
  As an operator
  I want only mismatched counts to appear in the CSV.

  Scenario: exporting mismatches for two roots
    Given a google root returns a count of 1
    And a microsoft root returns a count of 2
    When the comparison pipeline runs
    Then the CSV should contain two difference rows
