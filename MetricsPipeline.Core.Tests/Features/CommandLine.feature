Feature: Command Line
  In order to run drive comparisons from a shell
  As an operator
  I want to parse options and export mismatches.

  Scenario: defaulting google auth from environment
    Given environment variable GOOGLE_AUTH is set to "cred.json"
    When I parse the arguments "--ms-root m --google-root g"
    Then the parsed options should contain "cred.json"

  Scenario: root paths from environment
    Given MS_ROOT is set to "m"
    And GOOGLE_ROOT is set to "g"
    And OUTPUT_CSV is set to "out.csv"
    When I parse the arguments ""
    Then the options MsRoot should be "m"
    And the options GoogleRoot should be "g"
    And the output path should be "out.csv"

  Scenario: max dop from environment
    Given MS_ROOT is set to "m"
    And GOOGLE_ROOT is set to "g"
    And environment variable GOOGLE_AUTH is set to "cred.json"
    And OUTPUT_CSV is set to "out.csv"
    And MAX_DOP is set to "8"
    When I parse the arguments ""
    Then the max dop should be 8

  Scenario: exporting CSV results
    When I run the CLI pipeline
    Then the output should contain a CSV header
