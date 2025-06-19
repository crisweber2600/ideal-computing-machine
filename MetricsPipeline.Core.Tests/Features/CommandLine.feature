Feature: Command Line
  In order to run drive comparisons from a shell
  As an operator
  I want to parse options and export mismatches.

  Scenario: defaulting google auth from environment
    Given environment variable GOOGLE_AUTH is set to "cred.json"
    When I parse the arguments "--ms-root m --google-root g"
    Then the parsed options should contain "cred.json"

  Scenario: exporting CSV results
    When I run the CLI pipeline
    Then the output should contain a CSV header
